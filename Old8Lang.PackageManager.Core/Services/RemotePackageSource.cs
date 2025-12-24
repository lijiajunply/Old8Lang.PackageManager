using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using System.Net.Http.Json;
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
    public RemotePackageSource(string name, string source, string? apiKey = null, int timeout = 30)
    {
        Name = name;
        Source = source.TrimEnd('/');
        _baseUrl = Source;

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
        try
        {
            var url = $"{_baseUrl}/v3/search?q={Uri.EscapeDataString(searchTerm)}&prerelease={includePrerelease}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<PackageSearchResponse>(jsonContent, _jsonOptions);

            if (searchResponse?.Data == null)
            {
                return [];
            }

            return searchResponse.Data.Select(ToPackage).Where(p => p != null).Cast<Package>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching packages from remote source: {ex.Message}");
            return [];
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
        try
        {
            var url = $"{_baseUrl}/v3/package/{Uri.EscapeDataString(packageId)}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var packageResponse = JsonSerializer.Deserialize<PackageDetailResponse>(jsonContent, _jsonOptions);

            if (packageResponse?.Versions == null)
            {
                return [];
            }

            var versions = packageResponse.Versions
                .Where(v => includePrerelease || !v.IsPrerelease)
                .Select(v => v.Version);

            return versions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting package versions from remote source: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// 下载包
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <param name="version">包版本</param>
    /// <returns>包文件流</returns>
    /// <exception cref="FileNotFoundException">包不存在时抛出</exception>
    /// <exception cref="InvalidOperationException">下载失败时抛出</exception>
    public async Task<Stream> DownloadPackageAsync(string packageId, string version)
    {
        try
        {
            var url =
                $"{_baseUrl}/v3/package/{Uri.EscapeDataString(packageId)}/{Uri.EscapeDataString(version)}/download";
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException($"Package {packageId} version {version} not found in remote source.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Failed to download package {packageId} version {version}. Status: {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync();
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to download package {packageId} version {version}: {ex.Message}", ex);
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
        try
        {
            var url =
                $"{_baseUrl}/v3/package/{Uri.EscapeDataString(packageId)}?version={Uri.EscapeDataString(version)}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var packageResponse = JsonSerializer.Deserialize<PackageDetailResponse>(jsonContent, _jsonOptions);

            return packageResponse != null ? ToPackage(packageResponse) : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting package metadata from remote source: {ex.Message}");
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
            var response = await _httpClient.GetAsync($"{_baseUrl}/v3/index.json");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
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
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<PackageDependency> Dependencies { get; set; } = new();
    public List<ExternalDependencyInfo> ExternalDependencies { get; set; } = new();
    public Dictionary<string, string> LanguageMetadata { get; set; } = new();
    public DateTime PublishedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long DownloadCount { get; set; }
    public long Size { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public bool IsListed { get; set; }
    public bool IsPrerelease { get; set; }
    public List<PackageVersionInfo> Versions { get; set; } = new();
}

/// <summary>
/// 包版本信息
/// </summary>
[Serializable]
public class PackageVersionInfo
{
    public string Version { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public long DownloadCount { get; set; }
    public bool IsPrerelease { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public long Size { get; set; }
}

/// <summary>
/// 外部依赖信息
/// </summary>
[Serializable]
public class ExternalDependencyInfo
{
    public string DependencyType { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string VersionSpec { get; set; } = string.Empty;
    public string IndexUrl { get; set; } = string.Empty;
    public string ExtraIndexUrl { get; set; } = string.Empty;
    public bool IsDevDependency { get; set; }
}

#endregion