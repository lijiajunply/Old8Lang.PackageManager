using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using System.Text.Json;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 默认包安装器实现
/// </summary>
public class DefaultPackageInstaller : IPackageInstaller
{
    private readonly PackageSourceManager _sourceManager;
    private readonly IPackageResolver _resolver;
    
    public DefaultPackageInstaller(PackageSourceManager sourceManager, IPackageResolver resolver)
    {
        _sourceManager = sourceManager;
        _resolver = resolver;
    }
    
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
            var resolveResult = await _resolver.ResolveDependenciesAsync(packageId, version, _sourceManager.GetEnabledSources());
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
            var enabledSources = _sourceManager.GetEnabledSources();
            Package? package = null;
            
            foreach (var source in enabledSources)
            {
                try
                {
                    package = await source.GetPackageMetadataAsync(packageId, version);
                    if (package != null)
                    {
                        using var packageStream = await source.DownloadPackageAsync(packageId, version);
                        var packageFilePath = Path.Combine(packageDir, $"{packageId}.{version}.o8pkg");
                        
                        await using var fileStream = File.Create(packageFilePath);
                        await packageStream.CopyToAsync(fileStream);
                        
                        // 保存包元数据
                        package.FilePath = packageFilePath;
                        var metadataPath = Path.Combine(packageDir, $"{packageId}.{version}.metadata.json");
                        var json = JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true });
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
                var depInstallResult = await InstallPackageAsync(dependency.PackageId, dependency.VersionRange, installPath);
                if (!depInstallResult.Success && dependency.IsRequired)
                {
                    result.Success = false;
                    result.Message = $"Failed to install required dependency {dependency.PackageId}: {depInstallResult.Message}";
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
    
    public async Task<bool> UninstallPackageAsync(string packageId, string version, string installPath)
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
                
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uninstalling package {packageId} version {version}: {ex.Message}");
            return false;
        }
    }
    
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

