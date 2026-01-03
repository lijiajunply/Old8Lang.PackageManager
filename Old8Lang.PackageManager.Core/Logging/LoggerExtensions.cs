using Microsoft.Extensions.Logging;

namespace Old8Lang.PackageManager.Core.Logging;

/// <summary>
/// 结构化日志扩展方法
/// 提供针对包管理器各种操作的结构化日志记录功能
/// </summary>
public static class LoggerExtensions
{
    // Package Source 相关日志
    /// <summary>
    /// 记录正在搜索包的日志消息定义
    /// 日志级别: Information, 事件ID: 1001
    /// 消息模板: "Searching packages in source '{SourceName}' with term '{SearchTerm}'"
    /// </summary>
    private static readonly Action<ILogger, string, string, Exception?> _searchingPackages =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1001, nameof(SearchingPackages)),
            "Searching packages in source '{SourceName}' with term '{SearchTerm}'");

    /// <summary>
    /// 记录搜索完成的日志消息定义
    /// 日志级别: Information, 事件ID: 1002
    /// 消息模板: "Search completed in source '{SourceName}', found {ResultCount} packages"
    /// </summary>
    private static readonly Action<ILogger, string, int, Exception?> _searchCompleted =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(1002, nameof(SearchCompleted)),
            "Search completed in source '{SourceName}', found {ResultCount} packages");

    /// <summary>
    /// 记录搜索失败的日志消息定义
    /// 日志级别: Warning, 事件ID: 1003
    /// 消息模板: "Search failed in source '{SourceName}' for term '{SearchTerm}'"
    /// </summary>
    private static readonly Action<ILogger, string, string, Exception?> _searchFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1003, nameof(SearchFailed)),
            "Search failed in source '{SourceName}' for term '{SearchTerm}'");

    /// <summary>
    /// 记录正在下载包的日志消息定义
    /// 日志级别: Information, 事件ID: 1004
    /// 消息模板: "Downloading package '{PackageId}' version '{Version}' from source '{SourceName}'"
    /// </summary>
    private static readonly Action<ILogger, string, string, string, Exception?> _downloadingPackage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(1004, nameof(DownloadingPackage)),
            "Downloading package '{PackageId}' version '{Version}' from source '{SourceName}'");

    /// <summary>
    /// 记录包下载完成的日志消息定义
    /// 日志级别: Information, 事件ID: 1005
    /// 消息模板: "Package '{PackageId}' version '{Version}' downloaded successfully, size: {Size} bytes"
    /// </summary>
    private static readonly Action<ILogger, string, string, long, Exception?> _downloadCompleted =
        LoggerMessage.Define<string, string, long>(
            LogLevel.Information,
            new EventId(1005, nameof(DownloadCompleted)),
            "Package '{PackageId}' version '{Version}' downloaded successfully, size: {Size} bytes");

    /// <summary>
    /// 记录HTTP请求失败的日志消息定义
    /// 日志级别: Error, 事件ID: 1006
    /// 消息模板: "HTTP request failed for '{Url}' in source '{SourceName}', status code: {StatusCode}"
    /// </summary>
    private static readonly Action<ILogger, string, string, int, Exception?> _httpRequestFailed =
        LoggerMessage.Define<string, string, int>(
            LogLevel.Error,
            new EventId(1006, nameof(HttpRequestFailed)),
            "HTTP request failed for '{Url}' in source '{SourceName}', status code: {StatusCode}");

    /// <summary>
    /// 记录网络错误的日志消息定义
    /// 日志级别: Error, 事件ID: 1007
    /// 消息模板: "Network error occurred while accessing source '{SourceName}'"
    /// </summary>
    private static readonly Action<ILogger, string, Exception?> _networkError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1007, nameof(NetworkError)),
            "Network error occurred while accessing source '{SourceName}'");

    // Dependency Resolution 相关日志
    /// <summary>
    /// 记录正在解析依赖项的日志消息定义
    /// 日志级别: Information, 事件ID: 2001
    /// 消息模板: "Resolving dependencies for package '{PackageId}' version '{Version}'"
    /// </summary>
    private static readonly Action<ILogger, string, string, Exception?> _resolvingDependencies =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2001, nameof(ResolvingDependencies)),
            "Resolving dependencies for package '{PackageId}' version '{Version}'");

    /// <summary>
    /// 记录依赖项解析完成的日志消息定义
    /// 日志级别: Information, 事件ID: 2002
    /// 消息模板: "Dependencies resolved for package '{PackageId}', total {Count} dependencies"
    /// </summary>
    private static readonly Action<ILogger, string, int, Exception?> _dependenciesResolved =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(2002, nameof(DependenciesResolved)),
            "Dependencies resolved for package '{PackageId}', total {Count} dependencies");

    /// <summary>
    /// 记录版本冲突的日志消息定义
    /// 日志级别: Warning, 事件ID: 2003
    /// 消息模板: "Version conflict detected for package '{PackageId}': {ConflictDetails}"
    /// </summary>
    private static readonly Action<ILogger, string, string, Exception?> _versionConflict =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(2003, nameof(VersionConflict)),
            "Version conflict detected for package '{PackageId}': {ConflictDetails}");

    // Package Signature 相关日志
    /// <summary>
    /// 记录正在验证签名的日志消息定义
    /// 日志级别: Information, 事件ID: 3001
    /// 消息模板: "Verifying signature for package '{PackageId}' version '{Version}'"
    /// </summary>
    private static readonly Action<ILogger, string, string, Exception?> _verifyingSignature =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3001, nameof(VerifyingSignature)),
            "Verifying signature for package '{PackageId}' version '{Version}'");

    /// <summary>
    /// 记录签名验证成功的日志消息定义
    /// 日志级别: Information, 事件ID: 3002
    /// 消息模板: "Signature verified successfully for package '{PackageId}' version '{Version}', signed by '{Signer}'"
    /// </summary>
    private static readonly Action<ILogger, string, string, string, Exception?> _signatureValid =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(3002, nameof(SignatureValid)),
            "Signature verified successfully for package '{PackageId}' version '{Version}', signed by '{Signer}'");

    /// <summary>
    /// 记录签名验证失败的日志消息定义
    /// 日志级别: Error, 事件ID: 3003
    /// 消息模板: "Signature verification failed for package '{PackageId}' version '{Version}'"
    /// </summary>
    private static readonly Action<ILogger, string, string, Exception?> _signatureInvalid =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(3003, nameof(SignatureInvalid)),
            "Signature verification failed for package '{PackageId}' version '{Version}'");

    // Public extension methods
    
    /// <summary>
    /// 记录正在搜索包的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="sourceName">包源名称</param>
    /// <param name="searchTerm">搜索关键词</param>
    public static void SearchingPackages(this ILogger logger, string sourceName, string searchTerm) =>
        _searchingPackages(logger, sourceName, searchTerm, null);

    /// <summary>
    /// 记录搜索完成的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="sourceName">包源名称</param>
    /// <param name="resultCount">搜索结果数量</param>
    public static void SearchCompleted(this ILogger logger, string sourceName, int resultCount) =>
        _searchCompleted(logger, sourceName, resultCount, null);

    /// <summary>
    /// 记录搜索失败的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="sourceName">包源名称</param>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="ex">异常信息</param>
    public static void SearchFailed(this ILogger logger, string sourceName, string searchTerm, Exception ex) =>
        _searchFailed(logger, sourceName, searchTerm, ex);

    /// <summary>
    /// 记录正在下载包的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="sourceName">包源名称</param>
    /// <param name="packageId">包标识符</param>
    /// <param name="version">包版本</param>
    public static void DownloadingPackage(this ILogger logger, string sourceName, string packageId, string version) =>
        _downloadingPackage(logger, sourceName, packageId, version, null);

    /// <summary>
    /// 记录包下载完成的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="packageId">包标识符</param>
    /// <param name="version">包版本</param>
    /// <param name="size">包大小（字节）</param>
    public static void DownloadCompleted(this ILogger logger, string packageId, string version, long size) =>
        _downloadCompleted(logger, packageId, version, size, null);

    /// <summary>
    /// 记录HTTP请求失败的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="sourceName">包源名称</param>
    /// <param name="url">请求的URL</param>
    /// <param name="statusCode">HTTP状态码</param>
    /// <param name="ex">异常信息</param>
    public static void HttpRequestFailed(this ILogger logger, string sourceName, string url, int statusCode, Exception ex) =>
        _httpRequestFailed(logger, url, sourceName, statusCode, ex);

    /// <summary>
    /// 记录网络错误的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="sourceName">包源名称</param>
    /// <param name="ex">异常信息</param>
    public static void NetworkError(this ILogger logger, string sourceName, Exception ex) =>
        _networkError(logger, sourceName, ex);

    /// <summary>
    /// 记录正在解析依赖项的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="packageId">包标识符</param>
    /// <param name="version">包版本</param>
    public static void ResolvingDependencies(this ILogger logger, string packageId, string version) =>
        _resolvingDependencies(logger, packageId, version, null);

    /// <summary>
    /// 记录依赖项解析完成的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="packageId">包标识符</param>
    /// <param name="count">依赖项总数</param>
    public static void DependenciesResolved(this ILogger logger, string packageId, int count) =>
        _dependenciesResolved(logger, packageId, count, null);

    /// <summary>
    /// 记录版本冲突的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="packageId">包标识符</param>
    /// <param name="conflictDetails">冲突详情</param>
    /// <param name="ex">异常信息</param>
    public static void VersionConflict(this ILogger logger, string packageId, string conflictDetails, Exception ex) =>
        _versionConflict(logger, packageId, conflictDetails, ex);

    /// <summary>
    /// 记录正在验证签名的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="packageId">包标识符</param>
    /// <param name="version">包版本</param>
    public static void VerifyingSignature(this ILogger logger, string packageId, string version) =>
        _verifyingSignature(logger, packageId, version, null);

    /// <summary>
    /// 记录签名验证成功的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="packageId">包标识符</param>
    /// <param name="version">包版本</param>
    /// <param name="signer">签名者信息</param>
    public static void SignatureValid(this ILogger logger, string packageId, string version, string signer) =>
        _signatureValid(logger, packageId, version, signer, null);

    /// <summary>
    /// 记录签名验证失败的日志
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="packageId">包标识符</param>
    /// <param name="version">包版本</param>
    /// <param name="ex">异常信息</param>
    public static void SignatureInvalid(this ILogger logger, string packageId, string version, Exception ex) =>
        _signatureInvalid(logger, packageId, version, ex);
}