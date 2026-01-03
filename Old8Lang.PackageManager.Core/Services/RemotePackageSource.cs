using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Core.Exceptions;
using Old8Lang.PackageManager.Core.Logging;
using Old8Lang.PackageManager.Core.Resilience;
using Polly;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 远程包源 - 通过 HTTP API 从远程服务器获取包
/// </summary>
public class RemotePackageSource : IPackageSource, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<RemotePackageSource> _logger;
    private readonly ResiliencePipeline _httpRetryPipeline;
    private readonly ResiliencePipeline _downloadPipeline;

    /// <summary>
    /// 包源名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 包源地址
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 远程包源
    /// </summary>
    /// <param name="name">包源名称</param>
    /// <param name="source">远程服务器地址</param>
    /// <param name="apiKey">可选的API密钥</param>
    /// <param name="timeout">请求超时时间（秒）</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="retryOptions">重试选项（可选）</param>
    public RemotePackageSource(
        string name,
        string source,
        string? apiKey = null,
        int timeout = 30,
        ILogger<RemotePackageSource>? logger = null,
        ResiliencePolicies.HttpRetryOptions? retryOptions = null)
    {
        Name = name;
        Source = source.TrimEnd('/');
        _baseUrl = Source;
        _logger = logger ?? NullLogger<RemotePackageSource>.Instance;

        // 初始化弹性策略
        _httpRetryPipeline = ResiliencePolicies.CreateHttpRetryPipeline(retryOptions, _logger);
        _downloadPipeline = ResiliencePolicies.CreateDownloadPipeline(_logger);

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeout)
        };

        // 设置默认请求头
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Old8Lang.PackageManager/1.0.0");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// 搜索包
    /// </summary>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="includePrerelease">是否包含预发布版本</param>
    /// <returns>匹配的包列表</returns>
    public async Task<IEnumerable<Package>> SearchPackagesAsync(string searchTerm, bool includePrerelease = false)
    {
        _logger.SearchingPackages(Name, searchTerm);

        try
        {
            var url = $"{_baseUrl}/v3/search?q={Uri.EscapeDataString(searchTerm)}&prerelease={includePrerelease}";
            var response = await _httpRetryPipeline.ExecuteAsync(async ct =>
                await _httpClient.GetAsync(url, ct));

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                _logger.HttpRequestFailed(Name, url, statusCode,
                    new PackageSourceNetworkException(
                        $"Search request failed with status code {statusCode}",
                        statusCode, Name, _baseUrl));
                return [];
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<PackageSearchResponse>(jsonContent, _jsonOptions);

            if (searchResponse?.Data == null)
            {
                _logger.LogWarning("Search response is null or has no data for term '{SearchTerm}' in source '{SourceName}'",
                    searchTerm, Name);
                return [];
            }

            var packages = searchResponse.Data.Select(ToPackage).Where(p => p != null).Cast<Package>().ToList();
            _logger.SearchCompleted(Name, packages.Count);
            return packages;
        }
        catch (HttpRequestException ex)
        {
            _logger.NetworkError(Name, ex);
            throw new PackageSourceNetworkException(
                $"Network error occurred while searching packages in source '{Name}'",
                ex, null, Name, _baseUrl);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Search request timed out for term '{SearchTerm}' in source '{SourceName}'",
                searchTerm, Name);
            throw new PackageSourceNetworkException(
                $"Search request timed out in source '{Name}'",
                ex, null, Name, _baseUrl);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse search response for term '{SearchTerm}' in source '{SourceName}'",
                searchTerm, Name);
            throw new PackageParseException(
                $"Failed to parse search response from source '{Name}'", ex);
        }
        catch (Exception ex) when (ex is not PackageManagerException)
        {
            _logger.SearchFailed(Name, searchTerm, ex);
            throw new PackageSourceException(
                $"Unexpected error occurred while searching packages in source '{Name}'",
                ex, Name, _baseUrl);
        }
    }

    /// <summary>
    /// 获取包的可用版本
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <param name="includePrerelease">是否包含预发布版本</param>
    /// <returns>版本列表</returns>
    public async Task<IEnumerable<string>> GetPackageVersionsAsync(string packageId, bool includePrerelease = false)
    {
        _logger.LogInformation("Fetching versions for package '{PackageId}' from source '{SourceName}'",
            packageId, Name);

        try
        {
            var url = $"{_baseUrl}/v3/package/{Uri.EscapeDataString(packageId)}";
            var response = await _httpRetryPipeline.ExecuteAsync(async ct =>
                await _httpClient.GetAsync(url, ct));

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Package '{PackageId}' not found in source '{SourceName}'",
                    packageId, Name);
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                _logger.HttpRequestFailed(Name, url, statusCode,
                    new PackageSourceNetworkException(
                        $"Get versions request failed with status code {statusCode}",
                        statusCode, Name, _baseUrl));
                return [];
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var packageResponse = JsonSerializer.Deserialize<PackageDetailResponse>(jsonContent, _jsonOptions);

            if (packageResponse?.Versions == null)
            {
                _logger.LogWarning("Version response is null or has no versions for package '{PackageId}' in source '{SourceName}'",
                    packageId, Name);
                return [];
            }

            var versions = packageResponse.Versions
                .Where(v => includePrerelease || !v.IsPrerelease)
                .Select(v => v.Version)
                .ToList();

            _logger.LogInformation("Found {Count} versions for package '{PackageId}' in source '{SourceName}'",
                versions.Count, packageId, Name);
            return versions;
        }
        catch (HttpRequestException ex)
        {
            _logger.NetworkError(Name, ex);
            throw new PackageSourceNetworkException(
                $"Network error occurred while fetching versions for package '{packageId}' in source '{Name}'",
                ex, null, Name, _baseUrl);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Get versions request timed out for package '{PackageId}' in source '{SourceName}'",
                packageId, Name);
            throw new PackageSourceNetworkException(
                $"Get versions request timed out for package '{packageId}' in source '{Name}'",
                ex, null, Name, _baseUrl);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse versions response for package '{PackageId}' in source '{SourceName}'",
                packageId, Name);
            throw new PackageParseException(
                $"Failed to parse versions response for package '{packageId}' from source '{Name}'", ex);
        }
        catch (Exception ex) when (ex is not PackageManagerException)
        {
            _logger.LogError(ex, "Unexpected error while getting versions for package '{PackageId}' in source '{SourceName}'",
                packageId, Name);
            throw new PackageSourceException(
                $"Unexpected error occurred while getting versions for package '{packageId}' in source '{Name}'",
                ex, Name, _baseUrl);
        }
    }

    /// <summary>
    /// 下载包
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <param name="version">包版本</param>
    /// <returns>包文件流</returns>
    /// <exception cref="PackageNotFoundException">包不存在时抛出</exception>
    /// <exception cref="PackageSourceNetworkException">网络请求失败时抛出</exception>
    public async Task<Stream> DownloadPackageAsync(string packageId, string version)
    {
        _logger.DownloadingPackage(Name, packageId, version);

        try
        {
            var url = $"{_baseUrl}/v3/package/{Uri.EscapeDataString(packageId)}/{Uri.EscapeDataString(version)}/download";
            var response = await _downloadPipeline.ExecuteAsync(async ct =>
                await _httpClient.GetAsync(url, ct));

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var notFoundEx = new PackageNotFoundException(packageId, version);
                _logger.LogWarning(notFoundEx, "Package '{PackageId}' version '{Version}' not found in source '{SourceName}'",
                    packageId, version, Name);
                throw notFoundEx;
            }

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                var networkEx = new PackageSourceNetworkException(
                    $"Failed to download package '{packageId}' version '{version}' from source '{Name}'. Status: {response.StatusCode}",
                    statusCode, Name, _baseUrl);
                _logger.HttpRequestFailed(Name, url, statusCode, networkEx);
                throw networkEx;
            }

            var stream = await response.Content.ReadAsStreamAsync();
            var contentLength = response.Content.Headers.ContentLength ?? 0;
            _logger.DownloadCompleted(packageId, version, contentLength);

            return stream;
        }
        catch (PackageNotFoundException)
        {
            throw;
        }
        catch (PackageSourceNetworkException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.NetworkError(Name, ex);
            throw new PackageSourceNetworkException(
                $"Network error occurred while downloading package '{packageId}' version '{version}' from source '{Name}'",
                ex, null, Name, _baseUrl);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Download request timed out for package '{PackageId}' version '{Version}' in source '{SourceName}'",
                packageId, version, Name);
            throw new PackageSourceNetworkException(
                $"Download request timed out for package '{packageId}' version '{version}' in source '{Name}'",
                ex, null, Name, _baseUrl);
        }
        catch (Exception ex) when (ex is not PackageManagerException)
        {
            _logger.LogError(ex, "Unexpected error while downloading package '{PackageId}' version '{Version}' from source '{SourceName}'",
                packageId, version, Name);
            throw new PackageSourceException(
                $"Failed to download package '{packageId}' version '{version}' from source '{Name}': {ex.Message}",
                ex, Name, _baseUrl);
        }
    }

    /// <summary>
    /// 获取包元数据
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <param name="version">包版本</param>
    /// <returns>包元数据</returns>
    public async Task<Package?> GetPackageMetadataAsync(string packageId, string version)
    {
        _logger.LogDebug("Fetching metadata for package '{PackageId}' version '{Version}' from source '{SourceName}'",
            packageId, version, Name);

        try
        {
            var url = $"{_baseUrl}/v3/package/{Uri.EscapeDataString(packageId)}?version={Uri.EscapeDataString(version)}";
            var response = await _httpRetryPipeline.ExecuteAsync(async ct =>
                await _httpClient.GetAsync(url, ct));

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Package '{PackageId}' version '{Version}' not found in source '{SourceName}'",
                    packageId, version, Name);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                _logger.HttpRequestFailed(Name, url, statusCode,
                    new PackageSourceNetworkException(
                        $"Get metadata request failed with status code {statusCode}",
                        statusCode, Name, _baseUrl));
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var packageResponse = JsonSerializer.Deserialize<PackageDetailResponse>(jsonContent, _jsonOptions);

            if (packageResponse == null)
            {
                _logger.LogWarning("Metadata response is null for package '{PackageId}' version '{Version}' in source '{SourceName}'",
                    packageId, version, Name);
                return null;
            }

            _logger.LogDebug("Successfully fetched metadata for package '{PackageId}' version '{Version}' from source '{SourceName}'",
                packageId, version, Name);
            return ToPackage(packageResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error while fetching metadata for package '{PackageId}' version '{Version}' in source '{SourceName}'",
                packageId, version, Name);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Get metadata request timed out for package '{PackageId}' version '{Version}' in source '{SourceName}'",
                packageId, version, Name);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse metadata response for package '{PackageId}' version '{Version}' in source '{SourceName}'",
                packageId, version, Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting metadata for package '{PackageId}' version '{Version}' in source '{SourceName}'",
                packageId, version, Name);
            return null;
        }
    }

    /// <summary>
    /// 测试连接
    /// </summary>
    /// <returns>是否连接成功</returns>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogDebug("Testing connection to source '{SourceName}' at '{BaseUrl}'", Name, _baseUrl);
            var response = await _httpRetryPipeline.ExecuteAsync(async ct =>
                await _httpClient.GetAsync($"{_baseUrl}/v3/index.json", ct));
            var success = response.IsSuccessStatusCode;

            if (success)
            {
                _logger.LogInformation("Successfully connected to source '{SourceName}'", Name);
            }
            else
            {
                _logger.LogWarning("Connection test failed for source '{SourceName}', status code: {StatusCode}",
                    Name, response.StatusCode);
            }

            return success;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error during connection test for source '{SourceName}'", Name);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Connection test timed out for source '{SourceName}'", Name);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during connection test for source '{SourceName}'", Name);
            return false;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    #region 私有方法

    /// <summary>
    /// 将搜索结果转换为包模型
    /// </summary>
    private static Package? ToPackage(PackageSearchResult? result)
    {
        if (result == null) return null;

        return new Package
        {
            Id = result.PackageId,
            Version = result.Version,
            Description = result.Description,
            Author = result.Author,
            Tags = result.Tags,
            PublishedAt = result.PublishedAt,
            Size = 0, // 搜索结果中不包含大小信息
            Checksum = string.Empty, // 搜索结果中不包含校验和
            FilePath = string.Empty,
            Dependencies = new List<PackageDependency>()
        };
    }

    /// <summary>
    /// 将详细响应转换为包模型
    /// </summary>
    private static Package? ToPackage(PackageDetailResponse? response)
    {
        if (response == null) return null;

        return new Package
        {
            Id = response.PackageId,
            Version = response.Version,
            Description = response.Description,
            Author = response.Author,
            Tags = response.Tags,
            PublishedAt = response.PublishedAt,
            Size = response.Size,
            Checksum = response.Checksum,
            FilePath = string.Empty,
            Dependencies = response.Dependencies.Select(d => new PackageDependency
            {
                PackageId = d.PackageId,
                VersionRange = d.VersionRange,
                IsRequired = d.IsRequired
            }).ToList()
        };
    }

    #endregion
}

#region 响应模型

/// <summary>
/// 包搜索结果
/// </summary>
[Serializable]
public class PackageSearchResult
{
    /// <summary>
    /// 包标识符
    /// </summary>
    public string PackageId { get; set; } = string.Empty;

    /// <summary>
    /// 包版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 包语言
    /// </summary>
    public string Language { get; set; } = string.Empty;

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
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// 下载计数
    /// </summary>
    public long DownloadCount { get; set; }

    /// <summary>
    /// 是否为预发布版本
    /// </summary>
    public bool IsPrerelease { get; set; }
}

/// <summary>
/// 包搜索响应
/// </summary>
[Serializable]
public class PackageSearchResponse
{
    /// <summary>
    /// 总命中数
    /// </summary>
    public int TotalHits { get; set; }

    /// <summary>
    /// 搜索结果数据
    /// </summary>
    public List<PackageSearchResult> Data { get; set; } = [];
}

/// <summary>
/// 包详细信息响应
/// </summary>
[Serializable]
public class PackageDetailResponse
{
    /// <summary>
    /// 包标识符
    /// </summary>
    public string PackageId { get; set; } = string.Empty;

    /// 包版本
    public string Version { get; set; } = string.Empty;

    /// 包语言
    public string Language { get; set; } = string.Empty;

    /// 包描述
    public string Description { get; set; } = string.Empty;

    /// 包作者
    public string Author { get; set; } = string.Empty;

    /// 包许可证
    public string License { get; set; } = string.Empty;

    /// 包项目网址
    public string ProjectUrl { get; set; } = string.Empty;

    /// 包标签
    public List<string> Tags { get; set; } = [];

    /// 包依赖项
    public List<PackageDependency> Dependencies { get; set; } = [];

    /// 包外部依赖项
    public List<ExternalDependencyInfo> ExternalDependencies { get; set; } = [];

    /// 包语言元数据
    public Dictionary<string, string> LanguageMetadata { get; set; } = new();

    /// 包发布时间
    public DateTime PublishedAt { get; set; }

    /// 包更新时间
    public DateTime UpdatedAt { get; set; }

    /// 包下载计数
    public long DownloadCount { get; set; }

    /// 包大小
    public long Size { get; set; }

    /// 包校验和
    public string Checksum { get; set; } = string.Empty;

    /// 包是否已列出
    public bool IsListed { get; set; }

    /// 包是否为预发布版本
    public bool IsPrerelease { get; set; }

    /// 包版本信息
    public List<PackageVersionInfo> Versions { get; set; } = [];
}

/// <summary>
/// 包版本信息
/// </summary>
[Serializable]
public class PackageVersionInfo
{
    /// <summary>
    /// 包版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 包发布时间
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// 包下载计数
    /// </summary>
    public long DownloadCount { get; set; }

    /// <summary>
    /// 包是否为预发布版本
    /// </summary>
    public bool IsPrerelease { get; set; }

    /// <summary>
    /// 包校验和
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// 包大小
    /// </summary>
    public long Size { get; set; }
}

/// <summary>
/// 外部依赖信息
/// </summary>
[Serializable]
public class ExternalDependencyInfo
{
    /// <summary>
    /// 依赖类型
    /// </summary>
    public string DependencyType { get; set; } = string.Empty;

    /// <summary>
    /// 包标识符
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// 包版本范围
    /// </summary>
    public string VersionSpec { get; set; } = string.Empty;

    /// <summary>
    /// 包索引网址
    /// </summary>
    public string IndexUrl { get; set; } = string.Empty;

    /// <summary>
    /// 额外的索引网址
    /// </summary>
    public string ExtraIndexUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否为开发依赖
    /// </summary>
    public bool IsDevDependency { get; set; }
}

#endregion