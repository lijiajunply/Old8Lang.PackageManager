using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Old8Lang.PackageManager.Server.Models;
using Old8Lang.PackageManager.Server.Services;
using Old8Lang.PackageManager.Server.Configuration;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Server.Controllers;

/// <summary>
/// 包搜索和管理 API
/// </summary>
[ApiController]
[Route("v3")]
[Produces("application/json")]
public class PackagesController(
    IPackageSearchService searchService,
    IPackageManagementService packageService,
    IApiKeyService apiKeyService,
    ApiOptions apiOptions,
    ILogger<PackagesController> logger)
    : ControllerBase
{
    /// <summary>
    /// 搜索包
    /// </summary>
    /// <param name="q">搜索关键词</param>
    /// <param name="language">语言筛选 (old8lang, python)</param>
    /// <param name="skip">跳过数量</param>
    /// <param name="take">获取数量</param>
    /// <param name="prerelease">是否包含预发布版本</param>
    [HttpGet("search")]
    public async Task<ActionResult<PackageSearchResponse>> SearchPackages(
        [FromQuery] string q = "",
        [FromQuery] string? language = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] bool prerelease = false)
    {
        try
        {
            var result = await searchService.SearchAsync(q, language, skip, take);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "搜索包失败: {Query}", q);
            return StatusCode(500, ApiResponse<object>.ErrorResult("搜索包失败"));
        }
    }
    
    /// <summary>
    /// 获取热门包
    /// </summary>
    /// <param name="language">语言筛选 (old8lang, python)</param>
    /// <param name="take">获取数量</param>
    [HttpGet("popular")]
    public async Task<ActionResult<PackageSearchResponse>> GetPopularPackages(
        [FromQuery] string? language = null,
        [FromQuery] int take = 20)
    {
        try
        {
            var result = await searchService.GetPopularAsync(language, take);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取热门包失败");
            return StatusCode(500, ApiResponse<object>.ErrorResult("获取热门包失败"));
        }
    }
    
    /// <summary>
    /// 获取包详细信息
    /// </summary>
    /// <param name="id">包 ID</param>
    /// <param name="version">包版本（可选）</param>
    /// <param name="language">语言（可选）</param>
    [HttpGet("package/{id}")]
    public async Task<ActionResult<PackageDetailResponse>> GetPackageDetails(
        [FromRoute] string id,
        [FromQuery] string? version = null,
        [FromQuery] string? language = null)
    {
        try
        {
            PackageDetailResponse? result;
            
            if (!string.IsNullOrEmpty(version))
            {
                result = await searchService.GetPackageDetailsAsync(id, version, language);
            }
            else
            {
                result = await searchService.GetPackageDetailsAsync(id, language);
            }
            
            if (result == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("包不存在", "PACKAGE_NOT_FOUND"));
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取包详细信息失败: {PackageId} {Version} {Language}", id, version, language);
            return StatusCode(500, ApiResponse<object>.ErrorResult("获取包详细信息失败"));
        }
    }
    
    /// <summary>
    /// 上传包
    /// </summary>
    [HttpPost("package")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB
    [Authorize(Policy = "CanUpload")]
    public async Task<ActionResult<PackageDetailResponse>> UploadPackage([FromForm] PackageUploadRequest request)
    {
        try
        {
            // 验证 API 密钥
            if (apiOptions.RequireApiKey)
            {
                var apiKey = GetApiKeyFromRequest();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResult("需要 API 密钥", "API_KEY_REQUIRED"));
                }
                
                var keyEntity = await apiKeyService.ValidateApiKeyAsync(apiKey);
                if (keyEntity == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResult("无效的 API 密钥", "INVALID_API_KEY"));
                }
                
                // 检查权限
                if (!keyEntity.Scopes.Contains("package:write"))
                {
                    return Forbid();
                }
            }
            
            // 验证文件
            if (request.PackageFile == null || request.PackageFile.Length == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("未提供包文件", "NO_PACKAGE_FILE"));
            }
            
            // 验证文件扩展名
            var fileExtension = Path.GetExtension(request.PackageFile.FileName).ToLowerInvariant();
            if (fileExtension != ".o8pkg")
            {
                return BadRequest(ApiResponse<object>.ErrorResult("不支持的文件格式", "INVALID_FILE_FORMAT"));
            }
            
            // 上传包
            await using var packageStream = request.PackageFile.OpenReadStream();
            var packageEntity = await packageService.UploadPackageAsync(request, packageStream);
            
            var response = new PackageDetailResponse
            {
                PackageId = packageEntity.PackageId,
                Version = packageEntity.Version,
                Description = packageEntity.Description,
                Author = packageEntity.Author,
                License = packageEntity.License,
                ProjectUrl = packageEntity.ProjectUrl,
                Tags = packageEntity.PackageTags.Select(t => t.Tag).ToList(),
                Dependencies = packageEntity.PackageDependencies.Select(d => new PackageDependency
                {
                    PackageId = d.DependencyId,
                    VersionRange = d.VersionRange,
                    IsRequired = d.IsRequired
                }).ToList(),
                PublishedAt = packageEntity.PublishedAt,
                UpdatedAt = packageEntity.UpdatedAt,
                DownloadCount = packageEntity.DownloadCount,
                Size = packageEntity.Size,
                Checksum = packageEntity.Checksum,
                IsListed = packageEntity.IsListed,
                IsPrerelease = packageEntity.IsPrerelease,
                Versions = new List<PackageVersionInfo>()
            };
            
            return CreatedAtAction(nameof(GetPackageDetails), new { id = packageEntity.PackageId }, response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "包上传失败: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "包上传失败");
            return StatusCode(500, ApiResponse<object>.ErrorResult("包上传失败"));
        }
    }
    
    /// <summary>
    /// 删除包
    /// </summary>
    /// <param name="id">包 ID</param>
    /// <param name="version">包版本</param>
    [HttpDelete("package/{id}/{version}")]
    public async Task<ActionResult> DeletePackage([FromRoute] string id, [FromRoute] string version)
    {
        try
        {
            // 验证 API 密钥
            if (apiOptions.RequireApiKey)
            {
                var apiKey = GetApiKeyFromRequest();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResult("需要 API 密钥", "API_KEY_REQUIRED"));
                }
                
                var keyEntity = await apiKeyService.ValidateApiKeyAsync(apiKey);
                if (keyEntity == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResult("无效的 API 密钥", "INVALID_API_KEY"));
                }
                
                // 检查权限
                if (!keyEntity.Scopes.Contains("package:write"))
                {
                    return Forbid();
                }
            }
            
            var result = await packageService.DeletePackageAsync(id, version);
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("包不存在", "PACKAGE_NOT_FOUND"));
            }
            
            return Ok(ApiResponse<object>.SuccessResult(null, "包删除成功"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除包失败: {PackageId} {Version}", id, version);
            return StatusCode(500, ApiResponse<object>.ErrorResult("删除包失败"));
        }
    }
    
    /// <summary>
    /// 下载包
    /// </summary>
    /// <param name="id">包 ID</param>
    /// <param name="version">包版本</param>
    [HttpGet("package/{id}/{version}/download")]
    public async Task<IActionResult> DownloadPackage([FromRoute] string id, [FromRoute] string version)
    {
        try
        {
            var package = await packageService.GetPackageAsync(id, version);
            if (package == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("包不存在", "PACKAGE_NOT_FOUND"));
            }
            
            // 获取包文件流
            var packageStream = await GetPackageFileStreamAsync(id, version);
            if (packageStream == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("包文件不存在", "PACKAGE_FILE_NOT_FOUND"));
            }
            
            // 增加下载计数
            await packageService.IncrementDownloadCountAsync(id, version);
            
            var fileName = $"{id}.{version}.o8pkg";
            return File(packageStream, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "下载包失败: {PackageId} {Version}", id, version);
            return StatusCode(500, ApiResponse<object>.ErrorResult("下载包失败"));
        }
    }
    
    private string? GetApiKeyFromRequest()
    {
        // 从 Authorization header 获取
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader["Bearer ".Length..];
        }
        
        // 从查询参数获取
        if (Request.Query.TryGetValue("api_key", out var apiKey))
        {
            return apiKey.FirstOrDefault();
        }
        
        return null;
    }

    /// <summary>
    /// 获取当前用户 ID
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
    
    private async Task<Stream?> GetPackageFileStreamAsync(string packageId, string version)
    {
        // 这里应该调用包存储服务获取文件流
        // 为了简化示例，返回 null
        return null;
    }
}

/// <summary>
/// 服务索引控制器
/// </summary>
[ApiController]
[Route("v3/index.json")]
public class ServiceIndexController : ControllerBase
{
    private readonly ApiOptions _apiOptions;
    
    public ServiceIndexController(ApiOptions apiOptions)
    {
        _apiOptions = apiOptions;
    }
    
    /// <summary>
    /// 获取服务索引
    /// </summary>
    [HttpGet]
    public ActionResult<ServiceIndexResponse> GetServiceIndex()
    {
        var baseUrl = _apiOptions.BaseUrl.TrimEnd('/');
        var response = new ServiceIndexResponse
        {
            Version = _apiOptions.Version,
            Resources = new List<ServiceResource>
            {
                new()
                {
                    Id = $"{baseUrl}/v3/search",
                    Type = "SearchQueryService",
                    Comment = "查询包服务"
                },
                new()
                {
                    Id = $"{baseUrl}/v3/package/{{id}}",
                    Type = "PackageIndexService",
                    Comment = "包索引服务"
                },
                new()
                {
                    Id = $"{baseUrl}/v3/package/{{id}}/{{version}}/download",
                    Type = "PackageDownloadService",
                    Comment = "包下载服务"
                },
                new()
                {
                    Id = $"{baseUrl}/v3/package",
                    Type = "PackagePublishService",
                    Comment = "包发布服务"
                }
            }
        };
        
        return Ok(response);
    }
}