using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Core.Interfaces;

/// <summary>
/// 包安装器接口 - 负责包的安装和卸载
/// </summary>
public interface IPackageInstaller
{
    /// <summary>
    /// 安装包
    /// </summary>
    Task<InstallResult> InstallPackageAsync(string packageId, string version, string installPath);
    
    /// <summary>
    /// 卸载包
    /// </summary>
    Task<bool> UninstallPackageAsync(string packageId, string version, string installPath);
    
    /// <summary>
    /// 检查包是否已安装
    /// </summary>
    Task<bool> IsPackageInstalledAsync(string packageId, string version, string installPath);
    
    /// <summary>
    /// 获取已安装的包列表
    /// </summary>
    Task<IEnumerable<Package>> GetInstalledPackagesAsync(string installPath);
}

/// <summary>
/// 安装结果
/// </summary>
public class InstallResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Package? InstalledPackage { get; set; }
    public List<string> Warnings { get; set; } = new();
}