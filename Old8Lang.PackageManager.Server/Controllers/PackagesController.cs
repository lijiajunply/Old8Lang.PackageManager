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
    IPackageQualityService qualityService,
    IPackageDependencyService dependencyService,
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
            if (request.PackageFile.Length == 0)
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

    private async Task<Stream?> GetPackageFileStreamAsync(string packageId, string version)
    {
        // 这里应该调用包存储服务获取文件流
        // 为了简化示例，返回 null
        return null;
    }

    /// <summary>
    /// 获取包的质量评分
    /// </summary>
    /// <param name="id">包 ID</param>
    /// <param name="version">版本号</param>
    [HttpGet("package/{id}/{version}/quality")]
    public async Task<ActionResult<ApiResponse<PackageQualityScore>>> GetPackageQualityScore(string id, string version)
    {
        try
        {
            var score = await qualityService.GetQualityScoreAsync(id, version);
            if (score == null)
            {
                return NotFound(ApiResponse<PackageQualityScore>.ErrorResult("包不存在或质量评分未计算", "QUALITY_SCORE_NOT_FOUND"));
            }

            var qualityScoreDto = new PackageQualityScore
            {
                QualityScore = score.QualityScore,
                CompletenessScore = score.CompletenessScore,
                StabilityScore = score.StabilityScore,
                MaintenanceScore = score.MaintenanceScore,
                SecurityScore = score.SecurityScore,
                CommunityScore = score.CommunityScore,
                DocumentationScore = score.DocumentationScore,
                LastCalculatedAt = score.LastCalculatedAt
            };

            return Ok(ApiResponse<PackageQualityScore>.SuccessResult(qualityScoreDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取包质量评分失败: {PackageId} {Version}", id, version);
            return StatusCode(500, ApiResponse<PackageQualityScore>.ErrorResult("获取包质量评分失败"));
        }
    }

    /// <summary>
    /// 重新计算包的质量评分
    /// </summary>
    /// <param name="id">包 ID</param>
    /// <param name="version">版本号</param>
    [HttpPost("package/{id}/{version}/quality/recalculate")]
    public async Task<ActionResult<ApiResponse<PackageQualityScore>>> RecalculatePackageQualityScore(string id, string version)
    {
        try
        {
            // Validate API key
            var apiKey = GetApiKeyFromRequest();
            if (apiOptions.RequireApiKey && string.IsNullOrEmpty(apiKey))
            {
                return Unauthorized(ApiResponse<PackageQualityScore>.ErrorResult("需要提供有效的 API 密钥", "API_KEY_REQUIRED"));
            }

            if (apiOptions.RequireApiKey)
            {
                var keyEntity = await apiKeyService.ValidateApiKeyAsync(apiKey!);
                if (keyEntity == null)
                {
                    return Unauthorized(ApiResponse<PackageQualityScore>.ErrorResult("无效的 API 密钥", "INVALID_API_KEY"));
                }
            }

            var package = await packageService.GetPackageAsync(id, version);
            if (package == null)
            {
                return NotFound(ApiResponse<PackageQualityScore>.ErrorResult("包不存在", "PACKAGE_NOT_FOUND"));
            }

            var newScore = await qualityService.CalculateQualityScoreAsync(package);

            var qualityScoreDto = new PackageQualityScore
            {
                QualityScore = newScore.QualityScore,
                CompletenessScore = newScore.CompletenessScore,
                StabilityScore = newScore.StabilityScore,
                MaintenanceScore = newScore.MaintenanceScore,
                SecurityScore = newScore.SecurityScore,
                CommunityScore = newScore.CommunityScore,
                DocumentationScore = newScore.DocumentationScore,
                LastCalculatedAt = newScore.LastCalculatedAt
            };

            return Ok(ApiResponse<PackageQualityScore>.SuccessResult(qualityScoreDto, "质量评分已重新计算"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "重新计算包质量评分失败: {PackageId} {Version}", id, version);
            return StatusCode(500, ApiResponse<PackageQualityScore>.ErrorResult("重新计算包质量评分失败"));
        }
    }

    /// <summary>
    /// 重新计算所有包的质量评分 (管理员接口)
    /// </summary>
    [HttpPost("quality/recalculate-all")]
    public async Task<ActionResult<ApiResponse<object>>> RecalculateAllQualityScores()
    {
        try
        {
            // Validate API key with admin scope
            var apiKey = GetApiKeyFromRequest();
            if (string.IsNullOrEmpty(apiKey))
            {
                return Unauthorized(ApiResponse<object>.ErrorResult("需要提供有效的 API 密钥", "API_KEY_REQUIRED"));
            }

            var keyEntity = await apiKeyService.ValidateApiKeyAsync(apiKey);
            if (keyEntity == null || !keyEntity.Scopes.Contains("admin:all"))
            {
                return Unauthorized(ApiResponse<object>.ErrorResult("需要管理员权限", "ADMIN_REQUIRED"));
            }

            await qualityService.RecalculateAllScoresAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "所有包的质量评分已重新计算"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "重新计算所有包质量评分失败");
            return StatusCode(500, ApiResponse<object>.ErrorResult("重新计算所有包质量评分失败"));
        }
    }

    /// <summary>
    /// 获取包的依赖树
    /// </summary>
    /// <param name="id">包 ID</param>
    /// <param name="version">版本号</param>
    /// <param name="maxDepth">最大深度（默认10）</param>
    [HttpGet("package/{id}/{version}/dependencies/tree")]
    public async Task<ActionResult<ApiResponse<DependencyTreeResponse>>> GetDependencyTree(
        string id,
        string version,
        [FromQuery] int maxDepth = 10)
    {
        try
        {
            if (maxDepth < 1 || maxDepth > 50)
            {
                return BadRequest(ApiResponse<DependencyTreeResponse>.ErrorResult("最大深度必须在 1 到 50 之间", "INVALID_MAX_DEPTH"));
            }

            var tree = await dependencyService.GetDependencyTreeAsync(id, version, maxDepth);
            return Ok(ApiResponse<DependencyTreeResponse>.SuccessResult(tree));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "获取依赖树失败: 包不存在 {PackageId} {Version}", id, version);
            return NotFound(ApiResponse<DependencyTreeResponse>.ErrorResult(ex.Message, "PACKAGE_NOT_FOUND"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取依赖树失败: {PackageId} {Version}", id, version);
            return StatusCode(500, ApiResponse<DependencyTreeResponse>.ErrorResult("获取依赖树失败"));
        }
    }

    /// <summary>
    /// 获取包的依赖图（用于可视化）
    /// </summary>
    /// <param name="id">包 ID</param>
    /// <param name="version">版本号</param>
    /// <param name="maxDepth">最大深度（默认10）</param>
    [HttpGet("package/{id}/{version}/dependencies/graph")]
    public async Task<ActionResult<ApiResponse<DependencyGraphResponse>>> GetDependencyGraph(
        string id,
        string version,
        [FromQuery] int maxDepth = 10)
    {
        try
        {
            if (maxDepth < 1 || maxDepth > 50)
            {
                return BadRequest(ApiResponse<DependencyGraphResponse>.ErrorResult("最大深度必须在 1 到 50 之间", "INVALID_MAX_DEPTH"));
            }

            var graph = await dependencyService.GetDependencyGraphAsync(id, version, maxDepth);
            return Ok(ApiResponse<DependencyGraphResponse>.SuccessResult(graph));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "获取依赖图失败: 包不存在 {PackageId} {Version}", id, version);
            return NotFound(ApiResponse<DependencyGraphResponse>.ErrorResult(ex.Message, "PACKAGE_NOT_FOUND"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取依赖图失败: {PackageId} {Version}", id, version);
            return StatusCode(500, ApiResponse<DependencyGraphResponse>.ErrorResult("获取依赖图失败"));
        }
    }

    /// <summary>
    /// 检测包的循环依赖
    /// </summary>
    /// <param name="id">包 ID</param>
    /// <param name="version">版本号</param>
    [HttpGet("package/{id}/{version}/dependencies/circular")]
    public async Task<ActionResult<ApiResponse<List<string>>>> DetectCircularDependencies(
        string id,
        string version)
    {
        try
        {
            var circularPaths = await dependencyService.DetectCircularDependenciesAsync(id, version);
            return Ok(ApiResponse<List<string>>.SuccessResult(circularPaths));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "检测循环依赖失败: 包不存在 {PackageId} {Version}", id, version);
            return NotFound(ApiResponse<List<string>>.ErrorResult(ex.Message, "PACKAGE_NOT_FOUND"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "检测循环依赖失败: {PackageId} {Version}", id, version);
            return StatusCode(500, ApiResponse<List<string>>.ErrorResult("检测循环依赖失败"));
        }
    }
}

/// <summary>
/// 服务索引控制器
/// </summary>
[ApiController]
[Route("v3/index.json")]
public class ServiceIndexController(ApiOptions apiOptions) : ControllerBase
{
    /// <summary>
    /// 获取服务索引
    /// </summary>
    [HttpGet]
    public ActionResult<ServiceIndexResponse> GetServiceIndex()
    {
        var baseUrl = apiOptions.BaseUrl.TrimEnd('/');
        var response = new ServiceIndexResponse
        {
            Version = apiOptions.Version,
            Resources =
            [
                new ServiceResource
                {
                    Id = $"{baseUrl}/v3/search",
                    Type = "SearchQueryService",
                    Comment = "查询包服务"
                },

                new ServiceResource
                {
                    Id = $"{baseUrl}/v3/package/{{id}}",
                    Type = "PackageIndexService",
                    Comment = "包索引服务"
                },

                new ServiceResource
                {
                    Id = $"{baseUrl}/v3/package/{{id}}/{{version}}/download",
                    Type = "PackageDownloadService",
                    Comment = "包下载服务"
                },

                new ServiceResource
                {
                    Id = $"{baseUrl}/v3/package",
                    Type = "PackagePublishService",
                    Comment = "包发布服务"
                }
            ]
        };

        return Ok(response);
    }
}