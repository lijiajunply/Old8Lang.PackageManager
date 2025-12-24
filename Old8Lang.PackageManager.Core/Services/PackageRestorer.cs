using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 包还原管理器 - 负责根据配置文件还原所有包
/// </summary>
public class PackageRestorer
{
    private readonly IPackageConfigurationManager _configManager;
    private readonly IPackageInstaller _installer;
    private readonly PackageSourceManager _sourceManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="configManager"></param>
    /// <param name="installer"></param>
    /// <param name="sourceManager"></param>
    public PackageRestorer(
        IPackageConfigurationManager configManager,
        IPackageInstaller installer,
        PackageSourceManager sourceManager)
    {
        _configManager = configManager;
        _installer = installer;
        _sourceManager = sourceManager;
    }

    /// <summary>
    /// 还原所有包
    /// </summary>
    public async Task<RestoreResult> RestorePackagesAsync(string configPath, string? installPath = null)
    {
        var result = new RestoreResult();

        try
        {
            // 读取配置文件
            var configuration = await _configManager.ReadConfigurationAsync(configPath);
            var packagesDir = installPath ?? configuration.InstallPath;

            // 确保包源已配置
            await ConfigureSourcesAsync(configuration.Sources);

            // 获取包引用列表
            var references = await _configManager.GetPackageReferencesAsync(configPath);

            var packageReferences = references as PackageReference[] ?? references.ToArray();
            if (packageReferences.Length == 0)
            {
                result.Success = true;
                result.Message = "No packages to restore.";
                return result;
            }

            // 还原每个包
            foreach (var reference in packageReferences)
            {
                var installResult = await _installer.InstallPackageAsync(
                    reference.PackageId,
                    reference.Version,
                    packagesDir);

                if (installResult.Success)
                {
                    result.RestoredPackages.Add(new RestoredPackage
                    {
                        PackageId = reference.PackageId,
                        Version = reference.Version,
                        Success = true,
                        Message = installResult.Message
                    });
                }
                else
                {
                    result.RestoredPackages.Add(new RestoredPackage
                    {
                        PackageId = reference.PackageId,
                        Version = reference.Version,
                        Success = false,
                        Message = installResult.Message
                    });

                    result.HasErrors = true;
                    result.Warnings.Add($"Failed to restore {reference.PackageId}: {installResult.Message}");
                }

                result.Warnings.AddRange(installResult.Warnings);
            }

            result.Success = !result.HasErrors;
            result.Message = result.Success
                ? $"Successfully restored {result.RestoredPackages.Count(p => p.Success)} packages."
                : "Package restoration completed with errors.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Package restoration failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 清理未在配置文件中的包
    /// </summary>
    public async Task<CleanupResult> CleanupPackagesAsync(string configPath, string? installPath = null)
    {
        var result = new CleanupResult();

        try
        {
            // 读取配置文件
            var configuration = await _configManager.ReadConfigurationAsync(configPath);
            var packagesDir = installPath ?? configuration.InstallPath;

            // 获取配置文件中的包引用
            var references = await _configManager.GetPackageReferencesAsync(configPath);
            var expectedPackages = references.ToDictionary(r => r.PackageId, r => r.Version);

            // 获取已安装的包
            var installedPackages = await _installer.GetInstalledPackagesAsync(packagesDir);

            foreach (var installedPackage in installedPackages)
            {
                if (!expectedPackages.TryGetValue(installedPackage.Id, out var expectedVersion))
                {
                    // 包不在配置文件中，删除它
                    var success = await _installer.UninstallPackageAsync(
                        installedPackage.Id,
                        installedPackage.Version,
                        packagesDir);

                    result.RemovedPackages.Add(new RemovedPackage
                    {
                        PackageId = installedPackage.Id,
                        Version = installedPackage.Version,
                        Success = success
                    });
                }
                else if (installedPackage.Version != expectedVersion)
                {
                    // 版本不匹配，删除旧版本
                    var success = await _installer.UninstallPackageAsync(
                        installedPackage.Id,
                        installedPackage.Version,
                        packagesDir);

                    result.RemovedPackages.Add(new RemovedPackage
                    {
                        PackageId = installedPackage.Id,
                        Version = installedPackage.Version,
                        Success = success,
                        Reason = "Version mismatch"
                    });
                }
            }

            result.Success = true;
            result.Message = $"Cleanup completed. Removed {result.RemovedPackages.Count(p => p.Success)} packages.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Package cleanup failed: {ex.Message}";
        }

        return result;
    }

    private async Task ConfigureSourcesAsync(IEnumerable<PackageSource> sources)
    {
        foreach (var sourceConfig in sources)
        {
            var existingSource = _sourceManager.GetSource(sourceConfig.Name);
            if (existingSource == null)
            {
                // 根据源类型创建相应的包源
                if (sourceConfig.Source.StartsWith("http") || sourceConfig.Source.StartsWith("https"))
                {
                    existingSource = new RemotePackageSource(sourceConfig.Name, sourceConfig.Source)
                    {
                        IsEnabled = sourceConfig.IsEnabled
                    };
                    _sourceManager.AddSource(existingSource);
                }
                else
                {
                    var localSource = new LocalPackageSource(sourceConfig.Name, sourceConfig.Source)
                    {
                        IsEnabled = sourceConfig.IsEnabled
                    };
                    _sourceManager.AddSource(localSource);
                }
            }
            else
            {
                existingSource.IsEnabled = sourceConfig.IsEnabled;
            }
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// 还原结果
/// </summary>
[Serializable]
public class RestoreResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool HasErrors { get; set; }

    /// <summary>
    /// 已还原的包
    /// </summary>
    public List<RestoredPackage> RestoredPackages { get; set; } = [];

    /// <summary>
    /// 警告
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// 已还原的包
/// </summary>
[Serializable]
public class RestoredPackage
{
    /// <summary>
    /// 包ID
    /// </summary>
    public string PackageId { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 清理结果
/// </summary>
[Serializable]
public class CleanupResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 已删除的包
    /// </summary>
    public List<RemovedPackage> RemovedPackages { get; set; } = [];
}

/// <summary>
/// 已删除的包
/// </summary>
[Serializable]
public class RemovedPackage
{
    /// <summary>
    /// 包ID
    /// </summary>
    public string PackageId { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 失败原因
    /// </summary>
    public string? Reason { get; set; }
}