namespace Old8Lang.PackageManager.Core.Exceptions;

/// <summary>
/// 包管理器基础异常类
/// </summary>
public class PackageManagerException : Exception
{
    public PackageManagerException() { }

    public PackageManagerException(string message) : base(message) { }

    public PackageManagerException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// 包源异常 - 当包源操作失败时抛出
/// </summary>
public class PackageSourceException : PackageManagerException
{
    public string? SourceName { get; }
    public string? SourceUrl { get; }

    public PackageSourceException(string message, string? sourceName = null, string? sourceUrl = null)
        : base(message)
    {
        SourceName = sourceName;
        SourceUrl = sourceUrl;
    }

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
    public string PackageId { get; }
    public string? Version { get; }

    public PackageNotFoundException(string packageId, string? version = null)
        : base($"Package '{packageId}'{(version != null ? $" version '{version}'" : "")} not found.")
    {
        PackageId = packageId;
        Version = version;
    }

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
    public int? StatusCode { get; }

    public PackageSourceNetworkException(string message, int? statusCode = null, string? sourceName = null, string? sourceUrl = null)
        : base(message, sourceName, sourceUrl)
    {
        StatusCode = statusCode;
    }

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
    public string? FilePath { get; }

    public PackageParseException(string message, string? filePath = null)
        : base(message)
    {
        FilePath = filePath;
    }

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
    public string? PackageId { get; }
    public List<string> ConflictingVersions { get; }

    public DependencyResolutionException(string message, string? packageId = null, List<string>? conflictingVersions = null)
        : base(message)
    {
        PackageId = packageId;
        ConflictingVersions = conflictingVersions ?? new List<string>();
    }

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
    public string? PackageId { get; }
    public string? Version { get; }

    public PackageSignatureException(string message, string? packageId = null, string? version = null)
        : base(message)
    {
        PackageId = packageId;
        Version = version;
    }

    public PackageSignatureException(string message, Exception innerException, string? packageId = null, string? version = null)
        : base(message, innerException)
    {
        PackageId = packageId;
        Version = version;
    }
}
