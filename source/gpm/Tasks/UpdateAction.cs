using System;
using System.Linq;
using System.Threading.Tasks;
using gpm.core.Exceptions;
using gpm.core.Extensions;
using gpm.core.Models;
using gpm.core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace gpm.Tasks
{
    public static class UpdateAction
    {
        public static async Task<bool> Update(string name, bool global, string path, int? slot, string version, IHost host)
        {
            var serviceProvider = host.Services;
            ArgumentNullException.ThrowIfNull(serviceProvider);

            var dataBaseService = serviceProvider.GetRequiredService<IDataBaseService>();
            var libraryService = serviceProvider.GetRequiredService<ILibraryService>();

            // update here
            Upgrade.Action(host);

            // checks

            #region checks

            if (string.IsNullOrEmpty(name))
            {
                Log.Warning("No package name specified. To update all installed packages use gpm update --all");
                return false;
            }

            var package = dataBaseService.GetPackageFromName(name);
            if (package is null)
            {
                Log.Warning("Package {NameInner} not found in database", name);
                return false;
            }

            if (!libraryService.TryGetValue(package.Id, out _))
            {
                Log.Warning("[{Package}] Package not found in library. Use gpm install to install a package", package);
                return false;
            }

            if (!libraryService.IsInstalled(package))
            {
                Log.Warning("[{Package}] Package not installed. Use gpm install to install a package", package);
                return false;
            }

            if (global)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    Log.Warning(
                        "[{Package}] --global specifies the installation is user wide. Can't be combined with the --path option",
                        package);
                    return false;
                }

                if (slot is not null)
                {
                    Log.Warning(
                        "[{Package}] --global specifies the installation is user wide. Can't be combined with the --slot option",
                        package);
                    return false;
                }
            }

            if (slot is not null && !string.IsNullOrEmpty(path))
            {
                Log.Warning("[{Package}] please specify a --path option OR a --slot option, but not both", package);
                return false;
            }

            #endregion

            // try updating from --slot
            if (slot is not null)
            {
                if (!libraryService.IsInstalledInSlot(package, slot.Value))
                {
                    Log.Warning(
                        "[{Package}] Package not installed in slot {SlotIdx}. Use gpm install to install a package",
                        package, slot.ToString());
                    return false;
                }

                return await UpdatePackageInSlot(package, slot.Value, version, host);
            }

            // get slot from path
            if (!InstallAction.TryGetInstallPath(package, path, global, out var installPath))
            {
                return false;
            }
            if (!libraryService.IsInstalledAtLocation(package, installPath, out var slotIdx))
            {
                Log.Warning("[{Package}] Package not installed in {Path} Use gpm install to install a package", package,
                    installPath);
                return false;
            }

            return await UpdatePackageInSlot(package, slotIdx.Value, version , host);
        }

        private static async Task<bool> UpdatePackageInSlot(Package package, int slotIdx, string version, IHost host)
        {
            var serviceProvider = host.Services;
            ArgumentNullException.ThrowIfNull(serviceProvider);

            var libraryService = serviceProvider.GetRequiredService<ILibraryService>();
            var gitHubService = serviceProvider.GetRequiredService<IGitHubService>();
            var deploymentService = serviceProvider.GetRequiredService<IDeploymentService>();

            if (!libraryService.TryGetValue(package.Id, out var model))
            {
                Log.Warning("[{Package}] Package not found in library. Use gpm install to install a package", package);
                return false;
            }

            var releases = await gitHubService.GetReleasesForPackage(package);
            if (releases is null || !releases.Any())
            {
                Log.Warning("[{Package}] No releases found for package", package);
                return false;
            }
            if (!gitHubService.IsUpdateAvailable(package, releases, model.Slots[slotIdx].Version.NotNull()))
            {
                Log.Warning("[{Package}] No update available for package", package);
                return false;
            }

            // uninstall package in location
            // save slot location for later re-install
            var installPath = model.Slots[slotIdx].FullPath;
            Log.Debug("[{Package}] Removing installed package ...", package);
            if (await deploymentService.UninstallPackage(model.Key, slotIdx))
            {
                Log.Debug("[{Package}] Old package successfully removed", package);
            }
            else
            {
                Log.Warning("[{Package}] Failed to remove installed package. Aborting", package);
                return false;
            }

            // update to new version
            model.Slots.AddOrUpdate(slotIdx, new SlotManifest() {FullPath = installPath});
            Log.Information("[{Package}] Updating package ...", package);
            if (await deploymentService.InstallReleaseAsync(package, releases, version, slotIdx))
            {
                Log.Information("[{Package}] Package successfully updated to version {Version}", package,
                    model.Slots[slotIdx].Version);
                return true;
            }

            // handle dependencies
            // TODO

            Log.Warning("[{Package}] Failed to update package", package);
            return false;
        }
    }
}
