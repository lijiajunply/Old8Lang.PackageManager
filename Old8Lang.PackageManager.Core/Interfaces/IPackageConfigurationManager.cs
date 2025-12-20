using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Core.Interfaces;

/// <summary>
/// 包配置管理器接口 - 负责管理项目包配置文件
/// </summary>
public interface IPackageConfigurationManager
{
    /// <summary>
    /// 读取包配置文件
    /// </summary>
    Task<PackageConfiguration> ReadConfigurationAsync(string configPath);
    
    /// <summary>
    /// 写入包配置文件
    /// </summary>
    Task<bool> WriteConfigurationAsync(string configPath, PackageConfiguration configuration);
    
    /// <summary>
    /// 添加包引用到配置文件
    /// </summary>
    Task<bool> AddPackageReferenceAsync(string configPath, string packageId, string version);
    
    /// <summary>
    /// 从配置文件移除包引用
    /// </summary>
    Task<bool> RemovePackageReferenceAsync(string configPath, string packageId);
    
    /// <summary>
    /// 获取配置文件中的包引用列表
    /// </summary>
    Task<IEnumerable<PackageReference>> GetPackageReferencesAsync(string configPath);
}