using Microsoft.AspNetCore.Mvc;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Server.Models;

/// <summary>
/// 包搜索结果
/// </summary>
public class PackageSearchResult
{
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime PublishedAt { get; set; }
    public long DownloadCount { get; set; }
    public bool IsPrerelease { get; set; }
}

/// <summary>
/// 包搜索响应
/// </summary>
public class PackageSearchResponse
{
    public int TotalHits { get; set; }
    public List<PackageSearchResult> Data { get; set; } = new();
}

/// <summary>
/// 包详细信息响应
/// </summary>
public class PackageDetailResponse
{
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<PackageDependency> Dependencies { get; set; } = new();
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
public class PackageUploadRequest
{
    public IFormFile PackageFile { get; set; } = null!;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string License { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public bool IsPrerelease { get; set; }
}

/// <summary>
/// API 响应基类
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public static ApiResponse<T> SuccessResult(T data, string message = "操作成功")
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
public class ServiceResource
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}

/// <summary>
/// 包源索引响应
/// </summary>
public class ServiceIndexResponse
{
    public string Version { get; set; } = "3.0.0";
    public List<ServiceResource> Resources { get; set; } = new();
}