using Microsoft.Extensions.Logging;

namespace Old8Lang.PackageManager.Core.Logging;

/// <summary>
/// 结构化日志扩展方法
/// </summary>
public static class LoggerExtensions
{
    // Package Source 相关日志
    private static readonly Action<ILogger, string, string, Exception?> _searchingPackages =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1001, nameof(SearchingPackages)),
            "Searching packages in source '{SourceName}' with term '{SearchTerm}'");

    private static readonly Action<ILogger, string, int, Exception?> _searchCompleted =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(1002, nameof(SearchCompleted)),
            "Search completed in source '{SourceName}', found {ResultCount} packages");

    private static readonly Action<ILogger, string, string, Exception?> _searchFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1003, nameof(SearchFailed)),
            "Search failed in source '{SourceName}' for term '{SearchTerm}'");

    private static readonly Action<ILogger, string, string, string, Exception?> _downloadingPackage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(1004, nameof(DownloadingPackage)),
            "Downloading package '{PackageId}' version '{Version}' from source '{SourceName}'");

    private static readonly Action<ILogger, string, string, long, Exception?> _downloadCompleted =
        LoggerMessage.Define<string, string, long>(
            LogLevel.Information,
            new EventId(1005, nameof(DownloadCompleted)),
            "Package '{PackageId}' version '{Version}' downloaded successfully, size: {Size} bytes");

    private static readonly Action<ILogger, string, string, int, Exception?> _httpRequestFailed =
        LoggerMessage.Define<string, string, int>(
            LogLevel.Error,
            new EventId(1006, nameof(HttpRequestFailed)),
            "HTTP request failed for '{Url}' in source '{SourceName}', status code: {StatusCode}");

    private static readonly Action<ILogger, string, Exception?> _networkError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1007, nameof(NetworkError)),
            "Network error occurred while accessing source '{SourceName}'");

    // Dependency Resolution 相关日志
    private static readonly Action<ILogger, string, string, Exception?> _resolvingDependencies =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2001, nameof(ResolvingDependencies)),
            "Resolving dependencies for package '{PackageId}' version '{Version}'");

    private static readonly Action<ILogger, string, int, Exception?> _dependenciesResolved =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(2002, nameof(DependenciesResolved)),
            "Dependencies resolved for package '{PackageId}', total {Count} dependencies");

    private static readonly Action<ILogger, string, string, Exception?> _versionConflict =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(2003, nameof(VersionConflict)),
            "Version conflict detected for package '{PackageId}': {ConflictDetails}");

    // Package Signature 相关日志
    private static readonly Action<ILogger, string, string, Exception?> _verifyingSignature =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3001, nameof(VerifyingSignature)),
            "Verifying signature for package '{PackageId}' version '{Version}'");

    private static readonly Action<ILogger, string, string, string, Exception?> _signatureValid =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(3002, nameof(SignatureValid)),
            "Signature verified successfully for package '{PackageId}' version '{Version}', signed by '{Signer}'");

    private static readonly Action<ILogger, string, string, Exception?> _signatureInvalid =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(3003, nameof(SignatureInvalid)),
            "Signature verification failed for package '{PackageId}' version '{Version}'");

    // Public extension methods
    public static void SearchingPackages(this ILogger logger, string sourceName, string searchTerm) =>
        _searchingPackages(logger, sourceName, searchTerm, null);

    public static void SearchCompleted(this ILogger logger, string sourceName, int resultCount) =>
        _searchCompleted(logger, sourceName, resultCount, null);

    public static void SearchFailed(this ILogger logger, string sourceName, string searchTerm, Exception ex) =>
        _searchFailed(logger, sourceName, searchTerm, ex);

    public static void DownloadingPackage(this ILogger logger, string sourceName, string packageId, string version) =>
        _downloadingPackage(logger, sourceName, packageId, version, null);

    public static void DownloadCompleted(this ILogger logger, string packageId, string version, long size) =>
        _downloadCompleted(logger, packageId, version, size, null);

    public static void HttpRequestFailed(this ILogger logger, string sourceName, string url, int statusCode, Exception ex) =>
        _httpRequestFailed(logger, url, sourceName, statusCode, ex);

    public static void NetworkError(this ILogger logger, string sourceName, Exception ex) =>
        _networkError(logger, sourceName, ex);

    public static void ResolvingDependencies(this ILogger logger, string packageId, string version) =>
        _resolvingDependencies(logger, packageId, version, null);

    public static void DependenciesResolved(this ILogger logger, string packageId, int count) =>
        _dependenciesResolved(logger, packageId, count, null);

    public static void VersionConflict(this ILogger logger, string packageId, string conflictDetails, Exception ex) =>
        _versionConflict(logger, packageId, conflictDetails, ex);

    public static void VerifyingSignature(this ILogger logger, string packageId, string version) =>
        _verifyingSignature(logger, packageId, version, null);

    public static void SignatureValid(this ILogger logger, string packageId, string version, string signer) =>
        _signatureValid(logger, packageId, version, signer, null);

    public static void SignatureInvalid(this ILogger logger, string packageId, string version, Exception ex) =>
        _signatureInvalid(logger, packageId, version, ex);
}
