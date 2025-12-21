namespace Old8Lang.PackageManager.Core.Models;

/// <summary>
/// 表示一个 Old8Lang 包
/// </summary>
public class Package
{
    /// <summary>
    /// 包的唯一标识符
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 包版本
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// 包描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 包作者
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// 包标签
    /// </summary>
    public List<string> Tags { get; set; } = [];
    
    /// <summary>
    /// 依赖包列表
    /// </summary>
    public List<PackageDependency> Dependencies { get; set; } = [];
    
    /// <summary>
    /// 包文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// 包校验和
    /// </summary>
    public string Checksum { get; set; } = string.Empty;
    
    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime PublishedAt { get; set; }
    
    /// <summary>
    /// 包大小（字节）
    /// </summary>
    public long Size { get; set; }
}

/// <summary>
/// 包依赖关系
/// </summary>
public class PackageDependency
{
    /// <summary>
    /// 依赖包ID
    /// </summary>
    public string PackageId { get; set; } = string.Empty;
    
    /// <summary>
    /// 版本范围
    /// </summary>
    public string VersionRange { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否为必需依赖
    /// </summary>
    public bool IsRequired { get; set; } = true;
}