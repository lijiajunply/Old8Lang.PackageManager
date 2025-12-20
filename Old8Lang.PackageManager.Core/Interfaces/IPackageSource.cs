using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Core.Interfaces;

/// <summary>
/// 包源接口 - 定义包的获取方式
/// </summary>
public interface IPackageSource
{
    /// <summary>
    /// 包源名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 包源地址
    /// </summary>
    string Source { get; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    bool IsEnabled { get; set; }
    
    /// <summary>
    /// 搜索包
    /// </summary>
    Task<IEnumerable<Package>> SearchPackagesAsync(string searchTerm, bool includePrerelease = false);
    
    /// <summary>
    /// 获取包的可用版本
    /// </summary>
    Task<IEnumerable<string>> GetPackageVersionsAsync(string packageId, bool includePrerelease = false);
    
    /// <summary>
    /// 下载包
    /// </summary>
    Task<Stream> DownloadPackageAsync(string packageId, string version);
    
    /// <summary>
    /// 获取包元数据
    /// </summary>
    Task<Package?> GetPackageMetadataAsync(string packageId, string version);
}