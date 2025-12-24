namespace Old8Lang.PackageManager.Core.Interfaces;

/// <summary>
/// 包加载器接口 - 定义如何加载和执行包
/// </summary>
public interface IPackageLoader
{
    /// <summary>
    /// 加载包
    /// </summary>
    /// <param name="packagePath">包路径</param>
    /// <param name="context">加载上下文</param>
    /// <returns>加载的包对象</returns>
    Task<object?> LoadPackageAsync(string packagePath, PackageLoadContext context);

    /// <summary>
    /// 卸载包
    /// </summary>
    /// <param name="packageId">包ID</param>
    Task UnloadPackageAsync(string packageId);

    /// <summary>
    /// 检查包是否已加载
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <returns>是否已加载</returns>
    bool IsPackageLoaded(string packageId);

    /// <summary>
    /// 获取已加载的包
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <returns>包对象</returns>
    object? GetLoadedPackage(string packageId);
}

/// <summary>
/// 包加载上下文
/// </summary>
[Serializable]
public class PackageLoadContext
{
    /// <summary>
    /// 项目根目录
    /// </summary>
    public string ProjectRoot { get; set; } = string.Empty;

    /// <summary>
    /// 当前执行文件路径
    /// </summary>
    public string? CurrentFilePath { get; set; }

    /// <summary>
    /// 语言适配器
    /// </summary>
    public ILanguageAdapter? LanguageAdapter { get; set; }

    /// <summary>
    /// 额外的上下文数据
    /// </summary>
    public Dictionary<string, object> ContextData { get; set; } = new();
}