namespace Old8Lang.PackageManager.Core.Models;

/// <summary>
/// 包配置文件模型 - 类似 NuGet 的 packages.config 或 project.json
/// </summary>
public class PackageConfiguration
{
    /// <summary>
    /// 配置文件版本
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// 项目名称
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;
    
    /// <summary>
    /// 框架版本
    /// </summary>
    public string Framework { get; set; } = string.Empty;
    
    /// <summary>
    /// 包源列表
    /// </summary>
    public List<PackageSource> Sources { get; set; } = [];
    
    /// <summary>
    /// 包引用列表
    /// </summary>
    public List<PackageReference> References { get; set; } = [];
    
    /// <summary>
    /// 安装路径
    /// </summary>
    public string InstallPath { get; set; } = "packages";
}

/// <summary>
/// 包源配置
/// </summary>
public class PackageSource
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 源
    /// </summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 包引用
/// </summary>
public class PackageReference
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
    /// 是否为开发依赖
    /// </summary>
    public bool IsDevelopmentDependency { get; set; } = false;
    /// <summary>
    /// 目标框架
    /// </summary>
    public string? TargetFramework { get; set; }
}