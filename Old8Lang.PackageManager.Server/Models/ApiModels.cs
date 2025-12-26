using Microsoft.AspNetCore.Mvc;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Server.Models;

/// <summary>
/// 包搜索结果
/// </summary>
[Serializable]
public class PackageSearchResult
{
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime PublishedAt { get; set; }
    public long DownloadCount { get; set; }
    public bool IsPrerelease { get; set; }
    public PackageQualityScore? QualityScore { get; set; }
}

/// <summary>
/// 包搜索响应
/// </summary>
[Serializable]
public class PackageSearchResponse
{
    public int TotalHits { get; set; }
    public List<PackageSearchResult> Data { get; set; } = new();
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
    public PackageQualityScore? QualityScore { get; set; }
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
/// 包上传请求
/// </summary>
[Serializable]
public class PackageUploadRequest
{
    public IFormFile PackageFile { get; set; } = null!;
    public string Language { get; set; } = "old8lang"; // old8lang, python
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string License { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public bool IsPrerelease { get; set; }
    public string LanguageMetadata { get; set; } = string.Empty; // JSON 格式的语言特定元数据
    public List<ExternalDependencyInfo> ExternalDependencies { get; set; } = new();
}

/// <summary>
/// 外部依赖信息
/// </summary>
[Serializable]
public class ExternalDependencyInfo
{
    public string DependencyType { get; set; } = string.Empty; // pip, conda, npm, etc.
    public string PackageName { get; set; } = string.Empty;
    public string VersionSpec { get; set; } = string.Empty;
    public string IndexUrl { get; set; } = string.Empty;
    public string ExtraIndexUrl { get; set; } = string.Empty;
    public bool IsDevDependency { get; set; }
}

/// <summary>
/// API 响应基类
/// </summary>
[Serializable]
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResult(T? data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> ErrorResult(string message, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 包源服务资源
/// </summary>
[Serializable]
public class ServiceResource
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// 包源索引响应
/// </summary>
[Serializable]
public class ServiceIndexResponse
{
    public string Version { get; set; } = "3.0.0";
    public List<ServiceResource> Resources { get; set; } = new();
}

/// <summary>
/// 包质量评分信息
/// </summary>
[Serializable]
public class PackageQualityScore
{
    public double QualityScore { get; set; }
    public double CompletenessScore { get; set; }
    public double StabilityScore { get; set; }
    public double MaintenanceScore { get; set; }
    public double SecurityScore { get; set; }
    public double CommunityScore { get; set; }
    public double DocumentationScore { get; set; }
    public DateTime LastCalculatedAt { get; set; }
}
