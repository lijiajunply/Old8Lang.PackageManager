using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// Web 包源
/// </summary>
public class RemotePackageSource : IPackageSource
{
    /// <summary>
    /// 包源名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 包源地址
    /// </summary>
    public string Source { get; }
    
    /// <summary>
    /// 包源是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 创建一个 Web 包源
    /// </summary>
    public RemotePackageSource(string name, string source)
    {
        Name = name;
        Source = source;
    }

    /// <summary>
    /// 从远程地址搜索包
    /// </summary>
    /// <param name="searchTerm"></param>
    /// <param name="includePrerelease"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<Package>> SearchPackagesAsync(string searchTerm, bool includePrerelease = false)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// 获取指定包的所有版本
    /// </summary>
    public Task<IEnumerable<string>> GetPackageVersionsAsync(string packageId, bool includePrerelease = false)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 下载指定包的指定版本
    /// </summary>
    public Task<Stream> DownloadPackageAsync(string packageId, string version)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 获取指定包的元数据
    /// </summary>
    public Task<Package?> GetPackageMetadataAsync(string packageId, string version)
    {
        throw new NotImplementedException();
    }
}