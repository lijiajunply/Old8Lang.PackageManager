using Old8Lang.PackageManager.Server.Models;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// 包依赖关系分析服务接口
/// </summary>
public interface IPackageDependencyService
{
    /// <summary>
    /// 获取包的依赖树
    /// </summary>
    Task<DependencyTreeResponse> GetDependencyTreeAsync(string packageId, string version, int maxDepth = 10);

    /// <summary>
    /// 获取包的依赖图（用于可视化）
    /// </summary>
    Task<DependencyGraphResponse> GetDependencyGraphAsync(string packageId, string version, int maxDepth = 10);

    /// <summary>
    /// 检测循环依赖
    /// </summary>
    Task<List<string>> DetectCircularDependenciesAsync(string packageId, string version);
}
