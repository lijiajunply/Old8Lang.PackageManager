using Microsoft.EntityFrameworkCore;
using Old8Lang.PackageManager.Server.Configuration;
using Old8Lang.PackageManager.Server.Data;
using Old8Lang.PackageManager.Server.Models;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// API 密钥管理服务
/// </summary>
public interface IApiKeyService
{
    Task<ApiKeyEntity?> ValidateApiKeyAsync(string apiKey);
    Task<List<ApiKeyEntity>> GetAllApiKeysAsync();
    Task<ApiKeyEntity> CreateApiKeyAsync(string name, string description, string scopes, DateTime expiresAt);
    Task<bool> RevokeApiKeyAsync(int id);
    Task<bool> IncrementUsageAsync(string apiKey);
}

/// <summary>
/// API 密钥管理服务实现
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly PackageManagerDbContext _dbContext;
    private readonly ApiOptions _apiOptions;
    private readonly ILogger<ApiKeyService> _logger;
    
    public ApiKeyService(PackageManagerDbContext dbContext, ApiOptions apiOptions, ILogger<ApiKeyService> logger)
    {
        _dbContext = dbContext;
        _apiOptions = apiOptions;
        _logger = logger;
    }
    
    public async Task<ApiKeyEntity?> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }
        
        var keyEntity = await _dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.Key == apiKey && k.IsActive);
        
        if (keyEntity == null)
        {
            _logger.LogWarning("无效的 API 密钥: {ApiKey}", apiKey.Substring(0, Math.Min(8, apiKey.Length)) + "...");
            return null;
        }
        
        // 检查是否过期
        if (keyEntity.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("API 密钥已过期: {KeyId}", keyEntity.Id);
            return null;
        }
        
        // 增加使用计数
        keyEntity.UsageCount++;
        await _dbContext.SaveChangesAsync();
        
        return keyEntity;
    }
    
    public async Task<List<ApiKeyEntity>> GetAllApiKeysAsync()
    {
        return await _dbContext.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<ApiKeyEntity> CreateApiKeyAsync(string name, string description, string scopes, DateTime expiresAt)
    {
        var apiKey = new ApiKeyEntity
        {
            Name = name,
            Key = GenerateApiKey(),
            Description = description,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true,
            Scopes = scopes,
            UsageCount = 0
        };
        
        _dbContext.ApiKeys.Add(apiKey);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("API 密钥已创建: {Name} ({KeyId})", name, apiKey.Id);
        
        return apiKey;
    }
    
    public async Task<bool> RevokeApiKeyAsync(int id)
    {
        var apiKey = await _dbContext.ApiKeys.FindAsync(id);
        if (apiKey == null)
        {
            return false;
        }
        
        apiKey.IsActive = false;
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("API 密钥已撤销: {KeyId}", id);
        
        return true;
    }
    
    public async Task<bool> IncrementUsageAsync(string apiKey)
    {
        var keyEntity = await _dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.Key == apiKey && k.IsActive);
        
        if (keyEntity == null)
        {
            return false;
        }
        
        keyEntity.UsageCount++;
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
    
    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-");
    }
}

/// <summary>
/// 包搜索服务
/// </summary>
public interface IPackageSearchService
{
    Task<PackageSearchResponse> SearchAsync(string query, string? language = null, int skip = 0, int take = 20);
    Task<PackageSearchResponse> GetPopularAsync(string? language = null, int take = 20);
    Task<PackageDetailResponse?> GetPackageDetailsAsync(string packageId, string version, string? language = null);
    Task<PackageDetailResponse?> GetPackageDetailsAsync(string packageId, string? language = null);
}

/// <summary>
/// 包搜索服务实现
/// </summary>
public class PackageSearchService : IPackageSearchService
{
    private readonly IPackageManagementService _packageService;
    private readonly ApiOptions _apiOptions;
    
    public PackageSearchService(IPackageManagementService packageService, ApiOptions apiOptions)
    {
        _packageService = packageService;
        _apiOptions = apiOptions;
    }
    
    public async Task<PackageSearchResponse> SearchAsync(string query, string? language = null, int skip = 0, int take = 20)
    {
        var packages = await _packageService.SearchPackagesAsync(query, language, skip, take);
        var totalHits = await GetTotalHitsAsync(query, language);
        
        return new PackageSearchResponse
        {
            TotalHits = totalHits,
            Data = packages.Select(MapToSearchResult).ToList()
        };
    }
    
    public async Task<PackageSearchResponse> GetPopularAsync(string? language = null, int take = 20)
    {
        var packages = await _packageService.GetPopularPackagesAsync(language, take);
        
        return new PackageSearchResponse
        {
            TotalHits = packages.Count,
            Data = packages.Select(MapToSearchResult).ToList()
        };
    }
    
    public async Task<PackageDetailResponse?> GetPackageDetailsAsync(string packageId, string version, string? language = null)
    {
        var package = await _packageService.GetPackageAsync(packageId, version, language);
        if (package == null)
        {
            return null;
        }
        
        return MapToDetailResponse(package);
    }
    
    public async Task<PackageDetailResponse?> GetPackageDetailsAsync(string packageId, string? language = null)
    {
        var versions = await _packageService.GetAllVersionsAsync(packageId);
        if (!versions.Any())
        {
            return null;
        }
        
        // 筛选指定语言的包
        var filteredVersions = string.IsNullOrEmpty(language) 
            ? versions 
            : versions.Where(v => v.Language == language).ToList();
        
        if (!filteredVersions.Any())
        {
            return null;
        }
        
        var latestVersion = filteredVersions.OrderByDescending(v => v.PublishedAt).First();
        var detailResponse = MapToDetailResponse(latestVersion);
        
        // 添加所有版本信息
        detailResponse.Versions = filteredVersions.Select(v => new PackageVersionInfo
        {
            Version = v.Version,
            PublishedAt = v.PublishedAt,
            DownloadCount = v.DownloadCount,
            IsPrerelease = v.IsPrerelease,
            Checksum = v.Checksum,
            Size = v.Size
        }).ToList();
        
        return detailResponse;
    }
    
    private async Task<int> GetTotalHitsAsync(string query, string? language = null)
    {
        // 简化实现，实际应该使用更高效的计数查询
        var results = await _packageService.SearchPackagesAsync(query, language, 0, int.MaxValue);
        return results.Count;
    }
    
    private static PackageSearchResult MapToSearchResult(PackageEntity package)
    {
        return new PackageSearchResult
        {
            PackageId = package.PackageId,
            Version = package.Version,
            Language = package.Language,
            Description = package.Description,
            Author = package.Author,
            Tags = package.PackageTags.Select(t => t.Tag).ToList(),
            PublishedAt = package.PublishedAt,
            DownloadCount = package.DownloadCount,
            IsPrerelease = package.IsPrerelease
        };
    }
    
    private static PackageDetailResponse MapToDetailResponse(PackageEntity package)
    {
        return new PackageDetailResponse
        {
            PackageId = package.PackageId,
            Version = package.Version,
            Language = package.Language,
            Description = package.Description,
            Author = package.Author,
            License = package.License,
            ProjectUrl = package.ProjectUrl,
            Tags = package.PackageTags.Select(t => t.Tag).ToList(),
            Dependencies = package.PackageDependencies.Select(d => new PackageDependency
            {
                PackageId = d.DependencyId,
                VersionRange = d.VersionRange,
                IsRequired = d.IsRequired
            }).ToList(),
            ExternalDependencies = package.ExternalDependencies.Select(d => new ExternalDependencyInfo
            {
                DependencyType = d.DependencyType,
                PackageName = d.PackageName,
                VersionSpec = d.VersionSpec,
                IndexUrl = d.IndexUrl,
                ExtraIndexUrl = d.ExtraIndexUrl,
                IsDevDependency = d.IsDevDependency
            }).ToList(),
            PublishedAt = package.PublishedAt,
            UpdatedAt = package.UpdatedAt,
            DownloadCount = package.DownloadCount,
            Size = package.Size,
            Checksum = package.Checksum,
            IsListed = package.IsListed,
            IsPrerelease = package.IsPrerelease,
            Versions = new List<PackageVersionInfo>()
        };
    }
}