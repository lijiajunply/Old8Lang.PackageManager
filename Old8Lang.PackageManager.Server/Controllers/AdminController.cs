using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Old8Lang.PackageManager.Server.Models;
using Old8Lang.PackageManager.Server.Services;
using Old8Lang.PackageManager.Server.Configuration;

namespace Old8Lang.PackageManager.Server.Controllers;

/// <summary>
/// API 密钥管理控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 如果需要身份验证
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ApiOptions _apiOptions;
    private readonly ILogger<ApiKeysController> _logger;
    
    public ApiKeysController(
        IApiKeyService apiKeyService,
        ApiOptions apiOptions,
        ILogger<ApiKeysController> logger)
    {
        _apiKeyService = apiKeyService;
        _apiOptions = apiOptions;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取所有 API 密钥
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ApiKeyEntity>>> GetAllApiKeys()
    {
        try
        {
            var apiKeys = await _apiKeyService.GetAllApiKeysAsync();
            // 返回时不显示完整的密钥
            var safeApiKeys = apiKeys.Select(k => new ApiKeyEntity
            {
                Id = k.Id,
                Name = k.Name,
                Description = k.Description,
                CreatedAt = k.CreatedAt,
                ExpiresAt = k.ExpiresAt,
                IsActive = k.IsActive,
                Scopes = k.Scopes,
                UsageCount = k.UsageCount,
                Key = k.Key.Substring(0, Math.Min(8, k.Key.Length)) + "..." // 只显示前8位
            }).ToList();
            
            return Ok(safeApiKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 API 密钥列表失败");
            return StatusCode(500, ApiResponse<object>.ErrorResult("获取 API 密钥列表失败"));
        }
    }
    
    /// <summary>
    /// 创建新的 API 密钥
    /// </summary>
    /// <param name="request">创建请求</param>
    [HttpPost]
    public async Task<ActionResult<ApiKeyEntity>> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("名称不能为空"));
            }
            
            var expiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddYears(1);
            if (expiresAt <= DateTime.UtcNow)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("过期时间必须在未来"));
            }
            
            var scopes = string.IsNullOrWhiteSpace(request.Scopes) ? "package:read,package:write" : request.Scopes;
            
            var apiKey = await _apiKeyService.CreateApiKeyAsync(request.Name, request.Description, scopes, expiresAt);
            
            // 创建时返回完整的密钥
            return CreatedAtAction(nameof(GetAllApiKeys), new { id = apiKey.Id }, apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 API 密钥失败: {Name}", request.Name);
            return StatusCode(500, ApiResponse<object>.ErrorResult("创建 API 密钥失败"));
        }
    }
    
    /// <summary>
    /// 撤销 API 密钥
    /// </summary>
    /// <param name="id">API 密钥 ID</param>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> RevokeApiKey([FromRoute] int id)
    {
        try
        {
            var result = await _apiKeyService.RevokeApiKeyAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("API 密钥不存在", "API_KEY_NOT_FOUND"));
            }
            
            return Ok(ApiResponse<object>.SuccessResult(null, "API 密钥已撤销"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销 API 密钥失败: {KeyId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("撤销 API 密钥失败"));
        }
    }
    
    /// <summary>
    /// 验证 API 密钥
    /// </summary>
    /// <param name="request">验证请求</param>
    [HttpPost("validate")]
    [AllowAnonymous] // 允许匿名访问，用于验证密钥
    public async Task<ActionResult<ValidateApiKeyResponse>> ValidateApiKey([FromBody] ValidateApiKeyRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("API 密钥不能为空"));
            }
            
            var keyEntity = await _apiKeyService.ValidateApiKeyAsync(request.ApiKey);
            if (keyEntity == null)
            {
                return Ok(new ValidateApiKeyResponse
                {
                    IsValid = false,
                    Message = "无效的 API 密钥"
                });
            }
            
            return Ok(new ValidateApiKeyResponse
            {
                IsValid = true,
                Message = "API 密钥有效",
                Name = keyEntity.Name,
                Scopes = keyEntity.Scopes,
                ExpiresAt = keyEntity.ExpiresAt,
                UsageCount = keyEntity.UsageCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证 API 密钥失败");
            return StatusCode(500, ApiResponse<object>.ErrorResult("验证 API 密钥失败"));
        }
    }
}

/// <summary>
/// 统计信息控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IPackageManagementService _packageService;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<StatisticsController> _logger;
    
    public StatisticsController(
        IPackageManagementService packageService,
        IApiKeyService apiKeyService,
        ILogger<StatisticsController> logger)
    {
        _packageService = packageService;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取服务统计信息
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ServiceStatistics>> GetStatistics()
    {
        try
        {
            // 这里应该实现实际的统计逻辑
            var statistics = new ServiceStatistics
            {
                TotalPackages = 0,
                TotalDownloads = 0,
                TotalApiKeys = 0,
                ActiveApiKeys = 0,
                StorageUsage = 0,
                Uptime = TimeSpan.FromHours(24), // 示例值
                LastUpdated = DateTime.UtcNow
            };
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取统计信息失败");
            return StatusCode(500, ApiResponse<object>.ErrorResult("获取统计信息失败"));
        }
    }
    
    /// <summary>
    /// 获取包下载趋势
    /// </summary>
    /// <param name="days">统计天数</param>
    [HttpGet("downloads/trend")]
    public async Task<ActionResult<List<DownloadTrendData>>> GetDownloadTrend([FromQuery] int days = 30)
    {
        try
        {
            // 这里应该实现实际的下载趋势统计
            var trendData = new List<DownloadTrendData>();
            var now = DateTime.UtcNow;
            
            for (int i = days - 1; i >= 0; i--)
            {
                var date = now.AddDays(-i).Date;
                trendData.Add(new DownloadTrendData
                {
                    Date = date,
                    Downloads = Random.Shared.Next(10, 1000) // 示例数据
                });
            }
            
            return Ok(trendData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取下载趋势失败");
            return StatusCode(500, ApiResponse<object>.ErrorResult("获取下载趋势失败"));
        }
    }
}

// 请求和响应模型

/// <summary>
/// 创建 API 密钥请求
/// </summary>
public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// 验证 API 密钥请求
/// </summary>
public class ValidateApiKeyRequest
{
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// 验证 API 密钥响应
/// </summary>
public class ValidateApiKeyResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int UsageCount { get; set; }
}

/// <summary>
/// 服务统计信息
/// </summary>
public class ServiceStatistics
{
    public int TotalPackages { get; set; }
    public long TotalDownloads { get; set; }
    public int TotalApiKeys { get; set; }
    public int ActiveApiKeys { get; set; }
    public long StorageUsage { get; set; }
    public TimeSpan Uptime { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// 下载趋势数据
/// </summary>
public class DownloadTrendData
{
    public DateTime Date { get; set; }
    public int Downloads { get; set; }
}