namespace Old8Lang.PackageManager.Core.Exceptions;

/// <summary>
/// 包管理器基础异常类
/// </summary>
public class PackageManagerException : Exception
{
    /// <summary>
    /// 初始化 PackageManagerException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    public PackageManagerException(string message) : base(message) { }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 PackageManagerException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null</param>
    public PackageManagerException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// 包源异常 - 当包源操作失败时抛出
/// </summary>
public class PackageSourceException : PackageManagerException
{
    /// <summary>
    /// 获取包源的名称
    /// </summary>
    public string? SourceName { get; }
    
    /// <summary>
    /// 获取包源的URL
    /// </summary>
    public string? SourceUrl { get; }

    /// <summary>
    /// 初始化 PackageSourceException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="sourceName">包源的名称</param>
    /// <param name="sourceUrl">包源的URL</param>
    public PackageSourceException(string message, string? sourceName = null, string? sourceUrl = null)
        : base(message)
    {
        SourceName = sourceName;
        SourceUrl = sourceUrl;
    }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 PackageSourceException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null</param>
    /// <param name="sourceName">包源的名称</param>
    /// <param name="sourceUrl">包源的URL</param>
    public PackageSourceException(string message, Exception innerException, string? sourceName = null, string? sourceUrl = null)
        : base(message, innerException)
    {
        SourceName = sourceName;
        SourceUrl = sourceUrl;
    }
}

/// <summary>
/// 包不存在异常 - 当请求的包不存在时抛出
/// </summary>
public class PackageNotFoundException : PackageManagerException
{
    /// <summary>
    /// 获取包的ID
    /// </summary>
    public string PackageId { get; }
    
    /// <summary>
    /// 获取包的版本号（如果指定）
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// 初始化 PackageNotFoundException 类的新实例
    /// </summary>
    /// <param name="packageId">未找到的包的ID</param>
    /// <param name="version">未找到的包的版本（可选）</param>
    public PackageNotFoundException(string packageId, string? version = null)
        : base($"Package '{packageId}'{(version != null ? $" version '{version}'" : "")} not found.")
    {
        PackageId = packageId;
        Version = version;
    }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 PackageNotFoundException 类的新实例
    /// </summary>
    /// <param name="packageId">未找到的包的ID</param>
    /// <param name="version">未找到的包的版本（可选）</param>
    /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null</param>
    public PackageNotFoundException(string packageId, string? version, Exception innerException)
        : base($"Package '{packageId}'{(version != null ? $" version '{version}'" : "")} not found.", innerException)
    {
        PackageId = packageId;
        Version = version;
    }
}

/// <summary>
/// 网络异常 - 当网络请求失败时抛出
/// </summary>
public class PackageSourceNetworkException : PackageSourceException
{
    /// <summary>
    /// 获取HTTP状态码（如果可用）
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// 初始化 PackageSourceNetworkException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="statusCode">HTTP状态码（可选）</param>
    /// <param name="sourceName">包源的名称</param>
    /// <param name="sourceUrl">包源的URL</param>
    public PackageSourceNetworkException(string message, int? statusCode = null, string? sourceName = null, string? sourceUrl = null)
        : base(message, sourceName, sourceUrl)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 PackageSourceNetworkException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null</param>
    /// <param name="statusCode">HTTP状态码（可选）</param>
    /// <param name="sourceName">包源的名称</param>
    /// <param name="sourceUrl">包源的URL</param>
    public PackageSourceNetworkException(string message, Exception innerException, int? statusCode = null, string? sourceName = null, string? sourceUrl = null)
        : base(message, innerException, sourceName, sourceUrl)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// 包解析异常 - 当包元数据解析失败时抛出
/// </summary>
public class PackageParseException : PackageManagerException
{
    /// <summary>
    /// 获取导致异常的文件路径（如果可用）
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// 初始化 PackageParseException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="filePath">导致异常的文件路径（可选）</param>
    public PackageParseException(string message, string? filePath = null)
        : base(message)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 PackageParseException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null</param>
    /// <param name="filePath">导致异常的文件路径（可选）</param>
    public PackageParseException(string message, Exception innerException, string? filePath = null)
        : base(message, innerException)
    {
        FilePath = filePath;
    }
}

/// <summary>
/// 依赖解析异常 - 当依赖解析失败时抛出
/// </summary>
public class DependencyResolutionException : PackageManagerException
{
    /// <summary>
    /// 获取发生冲突的包ID（如果可用）
    /// </summary>
    public string? PackageId { get; }
    
    /// <summary>
    /// 获取冲突的版本列表
    /// </summary>
    public List<string> ConflictingVersions { get; }

    /// <summary>
    /// 初始化 DependencyResolutionException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="packageId">发生冲突的包ID（可选）</param>
    /// <param name="conflictingVersions">冲突的版本列表（可选）</param>
    public DependencyResolutionException(string message, string? packageId = null, List<string>? conflictingVersions = null)
        : base(message)
    {
        PackageId = packageId;
        ConflictingVersions = conflictingVersions ?? new List<string>();
    }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 DependencyResolutionException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null</param>
    /// <param name="packageId">发生冲突的包ID（可选）</param>
    public DependencyResolutionException(string message, Exception innerException, string? packageId = null)
        : base(message, innerException)
    {
        PackageId = packageId;
        ConflictingVersions = new List<string>();
    }
}

/// <summary>
/// 签名验证异常 - 当包签名验证失败时抛出
/// </summary>
public class PackageSignatureException : PackageManagerException
{
    /// <summary>
    /// 获取包的ID
    /// </summary>
    public string? PackageId { get; }
    
    /// <summary>
    /// 获取包的版本号（如果指定）
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// 初始化 PackageSignatureException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="packageId">包的ID（可选）</param>
    /// <param name="version">包的版本（可选）</param>
    public PackageSignatureException(string message, string? packageId = null, string? version = null)
        : base(message)
    {
        PackageId = packageId;
        Version = version;
    }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 PackageSignatureException 类的新实例
    /// </summary>
    /// <param name="message">描述错误的错误消息</param>
    /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null</param>
    /// <param name="packageId">包的ID（可选）</param>
    /// <param name="version">包的版本（可选）</param>
    public PackageSignatureException(string message, Exception innerException, string? packageId = null, string? version = null)
        : base(message, innerException)
    {
        PackageId = packageId;
        Version = version;
    }
}