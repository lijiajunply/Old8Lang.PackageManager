namespace Old8Lang.PackageManager.Core.Interfaces;

/// <summary>
/// 语言适配器接口 - 允许不同的脚本语言定制包加载行为
/// </summary>
public interface ILanguageAdapter
{
    /// <summary>
    /// 语言名称（如 "old8lang", "python", "javascript"）
    /// </summary>
    string LanguageName { get; }

    /// <summary>
    /// 支持的包文件扩展名（如 ".o8", ".py", ".js"）
    /// </summary>
    IEnumerable<string> SupportedFileExtensions { get; }

    /// <summary>
    /// 包配置文件名（如 "o8packages.json", "requirements.txt", "package.json"）
    /// </summary>
    string ConfigurationFileName { get; }

    /// <summary>
    /// 验证包格式是否有效
    /// </summary>
    /// <param name="packagePath">包路径</param>
    /// <returns>是否为有效的包</returns>
    bool ValidatePackageFormat(string packagePath);

    /// <summary>
    /// 从包中提取元数据
    /// </summary>
    /// <param name="packagePath">包路径</param>
    /// <returns>包元数据</returns>
    Task<PackageMetadata?> ExtractMetadataAsync(string packagePath);

    /// <summary>
    /// 安装包后的回调（如编译、初始化等）
    /// </summary>
    /// <param name="packagePath">已安装包的路径</param>
    Task OnPackageInstalledAsync(string packagePath);

    /// <summary>
    /// 卸载包前的回调（如清理、备份等）
    /// </summary>
    /// <param name="packagePath">要卸载包的路径</param>
    Task OnPackageUninstallingAsync(string packagePath);
}

/// <summary>
/// 包元数据
/// </summary>
public class PackageMetadata
{
    /// <summary>
    /// 包ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 依赖关系
    /// </summary>
    public List<DependencyInfo> Dependencies { get; set; } = [];

    /// <summary>
    /// 额外的自定义元数据
    /// </summary>
    public Dictionary<string, object> CustomMetadata { get; set; } = new();
}

/// <summary>
/// 依赖信息
/// </summary>
public class DependencyInfo
{
    /// <summary>
    /// 依赖包ID
    /// </summary>
    public string PackageId { get; set; } = string.Empty;

    /// <summary>
    /// 版本约束
    /// </summary>
    public string VersionConstraint { get; set; } = string.Empty;

    /// <summary>
    /// 是否可选
    /// </summary>
    public bool IsOptional { get; set; }
}
