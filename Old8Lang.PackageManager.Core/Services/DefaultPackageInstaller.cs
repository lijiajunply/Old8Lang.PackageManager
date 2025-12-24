using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using System.Text.Json;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 默认包安装器实现
/// </summary>
public class DefaultPackageInstaller(PackageSourceManager sourceManager, IPackageResolver resolver)
    : IPackageInstaller
{
    /// <summary>
    /// 下载包并安装
    /// </summary>
    /// <param name="packageId"></param>
    /// <param name="version"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<InstallResult> InstallPackageAsync(string packageId, string version, string installPath)
    {
        var result = new InstallResult();

        try
        {
            // 检查是否已安装
            if (await IsPackageInstalledAsync(packageId, version, installPath))
            {
                result.Success = false;
                result.Message = $"Package {packageId} version {version} is already installed.";
                return result;
            }

            // 解析依赖关系
            var resolveResult =
                await resolver.ResolveDependenciesAsync(packageId, version, sourceManager.GetEnabledSources());
            if (!resolveResult.Success)
            {
                result.Success = false;
                result.Message = $"Failed to resolve dependencies: {resolveResult.Message}";
                return result;
            }

            // 创建安装目录
            var packageDir = Path.Combine(installPath, packageId, version);
            Directory.CreateDirectory(packageDir);

            // 下载并安装包
            var enabledSources = sourceManager.GetEnabledSources();
            Package? package = null;

            foreach (var source in enabledSources)
            {
                try
                {
                    package = await source.GetPackageMetadataAsync(packageId, version);
                    if (package != null)
                    {
                        await using var packageStream = await source.DownloadPackageAsync(packageId, version);
                        var packageFilePath = Path.Combine(packageDir, $"{packageId}.{version}.o8pkg");

                        await using var fileStream = File.Create(packageFilePath);
                        await packageStream.CopyToAsync(fileStream);

                        // 保存包元数据
                        package.FilePath = packageFilePath;
                        var metadataPath = Path.Combine(packageDir, $"{packageId}.{version}.metadata.json");
                        var json = JsonSerializer.Serialize(package,
                            new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(metadataPath, json);

                        break;
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Failed to download from source '{source.Name}': {ex.Message}");
                }
            }

            if (package == null)
            {
                result.Success = false;
                result.Message = $"Package {packageId} version {version} not found in any source.";
                return result;
            }

            // 安装依赖包
            foreach (var dependency in resolveResult.ResolvedDependencies)
            {
                var depInstallResult =
                    await InstallPackageAsync(dependency.PackageId, dependency.VersionRange, installPath);
                if (!depInstallResult.Success && dependency.IsRequired)
                {
                    result.Success = false;
                    result.Message =
                        $"Failed to install required dependency {dependency.PackageId}: {depInstallResult.Message}";
                    return result;
                }

                result.Warnings.AddRange(depInstallResult.Warnings);
            }

            result.Success = true;
            result.Message = $"Package {packageId} version {version} installed successfully.";
            result.InstalledPackage = package;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Installation failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 卸载包
    /// </summary>
    /// <param name="packageId"></param>
    /// <param name="version"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public Task<bool> UninstallPackageAsync(string packageId, string version, string installPath)
    {
        try
        {
            var packageDir = Path.Combine(installPath, packageId, version);
            if (Directory.Exists(packageDir))
            {
                Directory.Delete(packageDir, true);

                // 如果包目录为空，删除它
                var parentDir = Path.Combine(installPath, packageId);
                if (Directory.Exists(parentDir) && !Directory.GetFileSystemEntries(parentDir).Any())
                {
                    Directory.Delete(parentDir);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uninstalling package {packageId} version {version}: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 检查包是否已安装
    /// </summary>
    /// <param name="packageId"></param>
    /// <param name="version"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<bool> IsPackageInstalledAsync(string packageId, string version, string installPath)
    {
        return await Task.Run(() =>
        {
            var packageDir = Path.Combine(installPath, packageId, version);
            return Directory.Exists(packageDir) &&
                   File.Exists(Path.Combine(packageDir, $"{packageId}.{version}.o8pkg")) &&
                   File.Exists(Path.Combine(packageDir, $"{packageId}.{version}.metadata.json"));
        });
    }

    /// <summary>
    /// 获取已安装的包
    /// </summary>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Package>> GetInstalledPackagesAsync(string installPath)
    {
        var packages = new List<Package>();

        if (!Directory.Exists(installPath))
            return packages;

        var packageDirs = Directory.GetDirectories(installPath);

        foreach (var packageDir in packageDirs)
        {
            var packageId = Path.GetFileName(packageDir);
            var versionDirs = Directory.GetDirectories(packageDir);

            foreach (var versionDir in versionDirs)
            {
                var version = Path.GetFileName(versionDir);
                var metadataFile = Path.Combine(versionDir, $"{packageId}.{version}.metadata.json");

                if (File.Exists(metadataFile))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(metadataFile);
                        var package = JsonSerializer.Deserialize<Package>(json);
                        if (package != null)
                        {
                            packages.Add(package);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading package metadata for {packageId} {version}: {ex.Message}");
                    }
                }
            }
        }

        return packages.OrderBy(p => p.Id).ThenBy(p => p.Version);
    }
}