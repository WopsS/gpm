using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using gpm.core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace gpm.Tasks
{
    /// <summary>
    /// To uninstall a global tool that was installed in the default location, use the --global option.
    /// To uninstall a global tool that was installed in a custom location, use the --tool-path option.
    /// To uninstall a local tool, omit the --global and --tool-path options.
    /// </summary>
    public static class RemoveAction
    {
        /// <summary>
        /// Update and remove a package
        /// </summary>
        /// <param name="name"></param>
        /// <param name="global"></param>
        /// <param name="path"></param>
        /// <param name="slot"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateAndRemove(string name, bool global, string path, int? slot, IHost host)
        {
            Upgrade.Action(host);

            return await Remove(name, global, path, slot, host);
        }

        /// <summary>
        /// Remove a package
        /// </summary>
        /// <param name="name"></param>
        /// <param name="global"></param>
        /// <param name="path"></param>
        /// <param name="slot"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public static async Task<bool> Remove(string name, bool global, string path, int? slot, IHost host)
        {
            var serviceProvider = host.Services;
            var libraryService = serviceProvider.GetRequiredService<ILibraryService>();
            var dataBaseService = serviceProvider.GetRequiredService<IDataBaseService>();
            var deploymentService = serviceProvider.GetRequiredService<IDeploymentService>();

            if (string.IsNullOrEmpty(name))
            {
                Log.Warning("No package name specified to install");
                return false;
            }

            // what if a package is removed from the database?
            // deprecate instead?
            var package = dataBaseService.GetPackageFromName(name);
            if (package is null)
            {
                Log.Warning("package {Name} not found in database", name);
                return false;
            }
            if (!libraryService.TryGetValue(package.Id, out var model))
            {
                Log.Warning("[{Package}] Package is not installed", package);
                return false;
            }
            if (!libraryService.IsInstalled(package))
            {
                Log.Warning("[{Package}] Package is not installed", package);
                return false;
            }

            // remove from default location
            if (global)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    Log.Warning("[{Package}] --global specifies the installation is user wide. Can't be combined with the --path option", package);
                    return false;
                }
                if (slot is not null)
                {
                    Log.Warning("[{Package}] --global specifies the installation is user wide. Can't be combined with the --slot option", package);
                    return false;
                }

                // uninstall from default slot
                var defaultDir = IAppSettings.GetDefaultInstallDir(package);
                foreach (var (key, value) in model.Slots)
                {
                    if (value.FullPath is not null && value.FullPath.Equals(defaultDir))
                    {
                        return await deploymentService.UninstallPackage(package, key);
                    }
                }

                Log.Warning("[{Package}] Package is not globally installed", package);
                return false;
            }

            // try uninstalling from --path
            if (!string.IsNullOrEmpty(path))
            {
                if (!Directory.Exists(path))
                {
                    Log.Warning("[{Package}] Path is not a valid directory: {Path}", package, path);
                    return false;
                }

                foreach (var (key, value) in model.Slots)
                {
                    if (value.FullPath is not null && value.FullPath.Equals(path))
                    {
                        return await deploymentService.UninstallPackage(package, key);
                    }
                }

                Log.Warning("[{Package}] Package is not installed in path: {Path}", package, path);
                return false;
            }

            // try uninstalling from --slot
            if (slot is not null)
            {
                return await deploymentService.UninstallPackage(package, slot.Value);
            }

            // try uninstalling from current dir
            var currentDir = Environment.CurrentDirectory;
            foreach (var (slotIdx, value) in model.Slots)
            {
                if (value.FullPath is not null && value.FullPath.Equals(currentDir))
                {
                    return await deploymentService.UninstallPackage(package, slotIdx);
                }
            }

            Log.Warning("[{Package}] Package is not installed in path: {Path}", package, currentDir);
            return false;
        }
    }
}
