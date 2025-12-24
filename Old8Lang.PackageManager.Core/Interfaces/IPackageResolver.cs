using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Core.Interfaces;

/// <summary>
/// 包解析器接口 - 负责依赖关系解析
/// </summary>
public interface IPackageResolver
{
    /// <summary>
    /// 解析包依赖关系
    /// </summary>
    Task<ResolveResult> ResolveDependenciesAsync(string packageId, string version, IEnumerable<IPackageSource> sources);
    
    /// <summary>
    /// 检查版本兼容性
    /// </summary>
    bool IsVersionCompatible(string requestedVersion, string availableVersion);
    
    /// <summary>
    /// 解析版本范围
    /// </summary>
    VersionRange ParseVersionRange(string versionRange);
}

/// <summary>
/// 依赖解析结果
/// </summary>
public class ResolveResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// 错误信息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// 解析后的依赖关系
    /// </summary>
    public List<PackageDependency> ResolvedDependencies { get; set; } = new();
    /// <summary>
    /// 冲突
    /// </summary>
    public List<string> Conflicts { get; set; } = new();
}

/// <summary>
/// 版本范围
/// </summary>
public class VersionRange
{
    /// <summary>
    /// 最小版本
    /// </summary>
    public string MinVersion { get; set; } = string.Empty;
    /// <summary>
    /// 最大版本
    /// </summary>
    public string MaxVersion { get; set; } = string.Empty;
    /// <summary>
    /// 包含最小版本
    /// </summary>
    public bool IncludeMinVersion { get; set; } = true;
    /// <summary>
    /// 包含最大版本
    /// </summary>
    public bool IncludeMaxVersion { get; set; } = true;
}