using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using gpm.core.Models;
using Octokit;

namespace gpm.core.Services
{
    public interface IDeploymentService
    {
        /// <summary>
        /// Download and install an asset file from a Github repo.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="releases"></param>
        /// <param name="requestedVersion"></param>
        /// <param name="slot"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>
        Task<bool> InstallReleaseAsync(
            Package package,
            IReadOnlyList<Release> releases,
            string? requestedVersion,
            int slot = 0);

        /// <summary>
        /// Installs a package from the cache location by version and exact filename
        /// </summary>
        /// <param name="package"></param>
        /// <param name="version"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        bool InstallPackageFromCache(Package package, string version, int slot = 0);

        /// <summary>
        /// Uninstalls a package from the system by slot
        /// </summary>
        /// <param name="package"></param>
        /// <param name="slotIdx"></param>
        Task<bool> UninstallPackage(Package package, int slotIdx = 0);

        /// <summary>
        /// Uninstalls a package from the system by slot
        /// </summary>
        /// <param name="key"></param>
        /// <param name="slotIdx"></param>
        /// <returns></returns>
        Task<bool> UninstallPackage(string key, int slotIdx = 0);

        /// <summary>
        /// Uninstalls a package from the system by slot
        /// </summary>
        /// <param name="model"></param>
        /// <param name="slotIdx"></param>
        Task<bool> UninstallPackage(PackageModel model, int slotIdx = 0);
    }
}
