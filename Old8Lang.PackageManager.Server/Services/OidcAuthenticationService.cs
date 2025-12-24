using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using System.Text.Json;
using Old8Lang.PackageManager.Server.Configuration;
using Old8Lang.PackageManager.Server.Models;
using Old8Lang.PackageManager.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// 用户服务
/// </summary>
public class UserService
{
    private readonly ILogger<UserService> _logger;
    private readonly PackageManagerDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public UserService(
        ILogger<UserService> logger,
        PackageManagerDbContext dbContext,
        IConfiguration configuration)
    {
        _logger = logger;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    /// <summary>
    /// 处理外部登录
    /// </summary>
    public async Task HandleExternalLoginAsync(OAuthCreatingTicketContext context, string provider)
    {
        try
        {
            var externalId = context.Identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value;
            var name = context.Identity?.FindFirst(ClaimTypes.Name)?.Value;
            var avatar = context.Identity?.FindFirst("avatar_url")?.Value;

            if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("External login failed: missing required claims");
                return;
            }

            // 查找或创建用户
            var user = await FindOrCreateUserAsync(externalId, email, name, avatar, provider);
            
            if (user != null)
            {
                // 更新最后登录时间
                user.LastLoginAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                // 添加用户 ID 到 Claims
                context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                context.Identity.AddClaim(new Claim("username", user.Username));
                
                _logger.LogInformation("User {UserId} logged in via {Provider}", user.Id, provider);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling external login for provider {Provider}", provider);
            throw;
        }
    }

    /// <summary>
    /// 查找或创建用户
    /// </summary>
    private async Task<UserEntity?> FindOrCreateUserAsync(string externalId, string email, string? name, string? avatar, string provider)
    {
        // 查找现有的外部登录
        var externalLogin = await _dbContext.UserExternalLogins
            .Include(el => el.User)
            .FirstOrDefaultAsync(el => el.ProviderKey == externalId && el.Provider == provider);

        if (externalLogin != null)
        {
            return externalLogin.User;
        }

        // 查找现有用户（通过邮箱）
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (existingUser != null)
        {
            // 添加外部登录到现有用户
            var newExternalLogin = new UserExternalLoginEntity
            {
                Provider = provider,
                ProviderKey = externalId,
                ProviderDisplayName = provider,
                UserId = existingUser.Id,
                SubjectId = externalId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.UserExternalLogins.Add(newExternalLogin);
            await _dbContext.SaveChangesAsync();
            return existingUser;
        }

        // 创建新用户
        var username = GenerateUsernameFromEmail(email);
        var newUser = new UserEntity
        {
            Username = username,
            Email = email,
            DisplayName = name ?? username,
            AvatarUrl = avatar,
            Provider = provider,
            SubjectId = externalId,
            IsEmailVerified = true, // OAuth 提供商已验证邮箱
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync();

        // 添加外部登录记录
        var externalLoginRecord = new UserExternalLoginEntity
        {
            Provider = provider,
            ProviderKey = externalId,
            ProviderDisplayName = provider,
            UserId = newUser.Id,
            SubjectId = externalId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserExternalLogins.Add(externalLoginRecord);
        await _dbContext.SaveChangesAsync();

        return newUser;
    }

    /// <summary>
    /// 从邮箱生成用户名
    /// </summary>
    private string GenerateUsernameFromEmail(string email)
    {
        var localPart = email.Split('@')[0];
        var username = localPart.ToLowerInvariant().Replace(".", "").Replace("_", "");
        
        // 确保用户名唯一
        var originalUsername = username;
        var counter = 1;
        
        while (_dbContext.Users.Any(u => u.Username == username))
        {
            username = $"{originalUsername}{counter}";
            counter++;
        }
        
        return username;
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    public async Task<UserEntity?> GetUserByIdAsync(int userId)
    {
        return await _dbContext.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    /// <summary>
    /// 获取用户的外部登录信息
    /// </summary>
    public async Task<List<UserExternalLoginEntity>> GetUserExternalLoginsAsync(int userId)
    {
        return await _dbContext.UserExternalLogins
            .Where(el => el.UserId == userId)
            .ToListAsync();
    }
}

/// <summary>
/// OIDC 认证服务
/// </summary>
public class OidcAuthenticationService
{
    private readonly ILogger<OidcAuthenticationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OidcConfiguration _oidcConfig;

    public OidcAuthenticationService(
        ILogger<OidcAuthenticationService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<OidcConfiguration> oidcConfig)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _oidcConfig = oidcConfig.Value;
    }

    /// <summary>
    /// 配置 OIDC 认证
    /// </summary>
    public void ConfigureOidcAuthentication(AuthenticationBuilder builder)
    {
        if (!_oidcConfig.Enabled)
        {
            _logger.LogInformation("OIDC authentication is disabled");
            return;
        }

        // 配置 Cookie 认证
        builder.AddCookie(options =>
        {
            options.Cookie.Name = "o8pm.auth";
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/access-denied";
            
            options.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = async context =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<PackageManagerDbContext>();
                    
                    var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out var id))
                    {
                        var user = await dbContext.Users.FindAsync(id);
                        if (user == null || !user.IsActive)
                        {
                            context.RejectPrincipal();
                        }
                    }
                }
            };
        });

        // 配置各 OAuth 提供商
        foreach (var (providerName, providerConfig) in _oidcConfig.Providers.Where(p => p.Value.Enabled))
        {
            ConfigureProvider(builder, providerName, providerConfig);
        }
    }

    /// <summary>
    /// 配置单个 OAuth 提供商
    /// </summary>
    private void ConfigureProvider(AuthenticationBuilder builder, string providerName, OidcProviderConfiguration config)
    {
        var schemeName = $"oidc-{providerName.ToLowerInvariant()}";
        
        if (providerName.Equals("GitHub", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureGitHubProvider(builder, schemeName, config);
        }
        else if (providerName.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureGoogleProvider(builder, schemeName, config);
        }
        else
        {
            ConfigureGenericProvider(builder, schemeName, config, providerName);
        }
    }

    /// <summary>
    /// 配置 GitHub OAuth
    /// </summary>
    private void ConfigureGitHubProvider(AuthenticationBuilder builder, string schemeName, OidcProviderConfiguration config)
    {
        builder.AddOAuth(schemeName, options =>
        {
            options.ClientId = config.ClientId;
            options.ClientSecret = config.ClientSecret;
            options.CallbackPath = !string.IsNullOrEmpty(config.CallbackPath) 
                ? new PathString(config.CallbackPath) 
                : new PathString("/signin-github");

            options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
            options.TokenEndpoint = "https://github.com/login/oauth/access_token";
            options.UserInformationEndpoint = "https://api.github.com/user";

            options.Scope.Clear();
            foreach (var scope in config.Scope)
            {
                options.Scope.Add(scope);
            }

            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey("avatar_url", "avatar_url");
            options.ClaimActions.MapJsonKey("login", "login");
            options.ClaimActions.MapJsonKey("bio", "bio");
            options.ClaimActions.MapJsonKey("company", "company");
            options.ClaimActions.MapJsonKey("location", "location");
            options.ClaimActions.MapJsonKey("blog", "blog");

            options.SaveTokens = true;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;

            options.Events = new OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                    await userService.HandleExternalLoginAsync(context, "GitHub");
                },
                OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    return context.Response.WriteAsync($"Authentication failed: {context.Failure?.Message}");
                }
            };
        });
    }

    /// <summary>
    /// 配置 Google OAuth
    /// </summary>
    private void ConfigureGoogleProvider(AuthenticationBuilder builder, string schemeName, OidcProviderConfiguration config)
    {
        builder.AddOAuth(schemeName, options =>
        {
            options.ClientId = config.ClientId;
            options.ClientSecret = config.ClientSecret;
            options.CallbackPath = !string.IsNullOrEmpty(config.CallbackPath)
                ? new PathString(config.CallbackPath)
                : new PathString("/signin-google");

            options.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
            options.TokenEndpoint = "https://oauth2.googleapis.com/token";
            options.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";

            options.Scope.Clear();
            foreach (var scope in config.Scope)
            {
                options.Scope.Add(scope);
            }

            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey("picture", "picture");

            options.SaveTokens = true;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;

            options.Events = new OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                    await userService.HandleExternalLoginAsync(context, "Google");
                },
                OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    return context.Response.WriteAsync($"Authentication failed: {context.Failure?.Message}");
                }
            };
        });
    }

    /// <summary>
    /// 配置通用 OAuth 提供商
    /// </summary>
    private void ConfigureGenericProvider(AuthenticationBuilder builder, string schemeName, OidcProviderConfiguration config, string providerName)
    {
        // 暂时简化实现，后续可以扩展为完整的 OIDC
        builder.AddOAuth(schemeName, options =>
        {
            options.ClientId = config.ClientId;
            options.ClientSecret = config.ClientSecret;
            options.CallbackPath = !string.IsNullOrEmpty(config.CallbackPath)
                ? new PathString(config.CallbackPath)
                : new PathString($"/signin-{providerName.ToLowerInvariant()}");

            // 这里需要根据具体提供商配置端点
            options.AuthorizationEndpoint = $"{config.Authority}/oauth/authorize";
            options.TokenEndpoint = $"{config.Authority}/oauth/token";
            options.UserInformationEndpoint = $"{config.Authority}/userinfo";

            options.Scope.Clear();
            foreach (var scope in config.Scope)
            {
                options.Scope.Add(scope);
            }

            options.SaveTokens = true;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;

            // 配置声明映射
            MapClaimActions(options.ClaimActions, config.ClaimsMapping);

            options.Events = new OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                    await userService.HandleExternalLoginAsync(context, providerName);
                },
                OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    return context.Response.WriteAsync($"Authentication failed: {context.Failure?.Message}");
                }
            };
        });
    }

    /// <summary>
    /// 映射声明动作
    /// </summary>
    private void MapClaimActions(Microsoft.AspNetCore.Authentication.OAuth.Claims.ClaimActionCollection claimActions, OidcClaimsMapping mapping)
    {
        claimActions.MapJsonKey(ClaimTypes.NameIdentifier, mapping.NameIdentifier);
        claimActions.MapJsonKey(ClaimTypes.Name, mapping.Name);
        claimActions.MapJsonKey(ClaimTypes.Email, mapping.Email);

        if (!string.IsNullOrEmpty(mapping.Avatar))
            claimActions.MapJsonKey("avatar_url", mapping.Avatar);

        if (!string.IsNullOrEmpty(mapping.Username))
            claimActions.MapJsonKey("preferred_username", mapping.Username);

        if (!string.IsNullOrEmpty(mapping.Website))
            claimActions.MapJsonKey("website", mapping.Website);

        if (!string.IsNullOrEmpty(mapping.Bio))
            claimActions.MapJsonKey("profile", mapping.Bio);

        if (!string.IsNullOrEmpty(mapping.Company))
            claimActions.MapJsonKey("organization", mapping.Company);

        if (!string.IsNullOrEmpty(mapping.Location))
            claimActions.MapJsonKey("locale", mapping.Location);
    }
}