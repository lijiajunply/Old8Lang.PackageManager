using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Old8Lang.PackageManager.Server.Models;
using Old8Lang.PackageManager.Server.Data;
using Old8Lang.PackageManager.Server.Services;

namespace Old8Lang.PackageManager.Server.Controllers;

/// <summary>
/// 用户认证控制器
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly PackageManagerDbContext _dbContext;
    private readonly UserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        PackageManagerDbContext dbContext,
        UserService userService,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { error = "Invalid user ID" });
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        var externalLogins = await _userService.GetUserExternalLoginsAsync(userId);

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            displayName = user.DisplayName,
            avatarUrl = user.AvatarUrl,
            bio = user.Bio,
            websiteUrl = user.WebsiteUrl,
            company = user.Company,
            location = user.Location,
            isEmailVerified = user.IsEmailVerified,
            isActive = user.IsActive,
            isAdmin = user.IsAdmin,
            createdAt = user.CreatedAt,
            lastLoginAt = user.LastLoginAt,
            packageCount = user.PackageCount,
            totalDownloads = user.TotalDownloads,
            usedStorage = user.UsedStorage,
            preferredLanguage = user.PreferredLanguage,
            emailNotificationsEnabled = user.EmailNotificationsEnabled,
            externalLogins = externalLogins.Select(el => new
            {
                provider = el.Provider,
                providerDisplayName = el.ProviderDisplayName,
                createdAt = el.CreatedAt
            }).ToList()
        });
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// 获取可用的登录提供商
    /// </summary>
    [HttpGet("providers")]
    public IActionResult GetAuthProviders()
    {
        var config = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var oidcConfig = config.GetSection("Authentication:OIDC");
        
        var providers = new List<object>();
        
        foreach (var provider in oidcConfig.GetSection("Providers").GetChildren())
        {
            var enabled = provider.GetValue<bool>("Enabled");
            if (enabled)
            {
                providers.Add(new
                {
                    name = provider.Key,
                    displayName = provider.Key,
                    callbackPath = provider.GetValue<string>("CallbackPath"),
                    scopes = provider.GetSection("Scope").Get<List<string>>() ?? new List<string>()
                });
            }
        }

        return Ok(new { providers });
    }

    /// <summary>
    /// 启动外部登录
    /// </summary>
    [HttpPost("login/{provider}")]
    public IActionResult ExternalLogin(string provider, [FromQuery] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            Items = { { "returnUrl", returnUrl ?? "/" } }
        };

        return Challenge(properties, $"oidc-{provider.ToLowerInvariant()}");
    }

    /// <summary>
    /// 外部登录回调
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync();
        if (!result.Succeeded)
        {
            return Redirect("/login?error=external_login_failed");
        }

        var userIdClaim = result.Principal?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Redirect("/login?error=invalid_user_id");
        }

        // 更新用户最后登录时间
        var user = await _userService.GetUserByIdAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("User {UserId} ({Username}) logged in successfully", user.Id, user.Username);
        }

        return LocalRedirect(returnUrl ?? "/");
    }
}