using System.ComponentModel.DataAnnotations;

namespace Old8Lang.PackageManager.Server.Configuration;

/// <summary>
/// OIDC 配置模型
/// </summary>
public class OidcConfiguration
{
    public bool Enabled { get; set; } = true;
    
    public string DefaultScheme { get; set; } = "OpenIdConnect";
    
    public Dictionary<string, OidcProviderConfiguration> Providers { get; set; } = new();
}

/// <summary>
/// OIDC 提供商配置模型
/// </summary>
public class OidcProviderConfiguration
{
    public bool Enabled { get; set; } = false;
    
    [Required]
    public string ClientId { get; set; } = string.Empty;
    
    [Required]
    public string ClientSecret { get; set; } = string.Empty;
    
    [Required]
    public string Authority { get; set; } = string.Empty;
    
    public string CallbackPath { get; set; } = string.Empty;
    
    public List<string> Scope { get; set; } = new();
    
    public OidcClaimsMapping ClaimsMapping { get; set; } = new();
    
    public Dictionary<string, string> AdditionalParameters { get; set; } = new();
}

/// <summary>
/// OIDC 声明映射配置模型
/// </summary>
public class OidcClaimsMapping
{
    [Required]
    public string NameIdentifier { get; set; } = "sub";
    
    [Required]
    public string Name { get; set; } = "name";
    
    [Required]
    public string Email { get; set; } = "email";
    
    public string? Avatar { get; set; }
    
    public string? Username { get; set; }
    
    public string? Website { get; set; }
    
    public string? Bio { get; set; }
    
    public string? Company { get; set; }
    
    public string? Location { get; set; }
}

/// <summary>
/// 认证配置模型
/// </summary>
public class AuthenticationConfiguration
{
    public bool EnableRegistration { get; set; } = true;
    
    public bool RequireEmailVerification { get; set; } = false;
    
    public bool AllowGuestUpload { get; set; } = false;
    
    public int MaxPackagesPerUser { get; set; } = 100;
    
    public long MaxPackageSizePerUser { get; set; } = 1073741824; // 1GB
    
    public OidcConfiguration OIDC { get; set; } = new();
    
    public List<string> AllowedEmailDomains { get; set; } = new();
    
    public List<string> BlockedEmailDomains { get; set; } = new();
    
    public bool RequireEmailWhitelist { get; set; } = false;
}

/// <summary>
/// IdentityServer 配置模型
/// </summary>
public class IdentityServerConfiguration
{
    public string Issuer { get; set; } = "https://localhost:5001";
    
    public Dictionary<string, ClientConfiguration> Clients { get; set; } = new();
    
    public ResourcesConfiguration Resources { get; set; } = new();
}

/// <summary>
/// 客户端配置模型
/// </summary>
public class ClientConfiguration
{
    [Required]
    public string ClientId { get; set; } = string.Empty;
    
    public string? ClientSecret { get; set; }
    
    public List<string> AllowedGrantTypes { get; set; } = new();
    
    public List<string> RedirectUris { get; set; } = new();
    
    public List<string> PostLogoutRedirectUris { get; set; } = new();
    
    public List<string> AllowedScopes { get; set; } = new();
    
    public bool AllowAccessTokensViaBrowser { get; set; } = false;
    
    public bool RequireConsent { get; set; } = true;
    
    public bool RequireClientSecret { get; set; } = true;
    
    public int? AccessTokenLifetime { get; set; } = 3600; // 1 hour
    
    public int? IdentityTokenLifetime { get; set; } = 300; // 5 minutes
}

/// <summary>
/// 资源配置模型
/// </summary>
public class ResourcesConfiguration
{
    public ApiResourceConfiguration Api { get; set; } = new();
}

/// <summary>
/// API 资源配置模型
/// </summary>
public class ApiResourceConfiguration
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string DisplayName { get; set; } = string.Empty;
    
    public List<ScopeConfiguration> Scopes { get; set; } = new();
}

/// <summary>
/// 作用域配置模型
/// </summary>
public class ScopeConfiguration
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? DisplayName { get; set; }
    
    public string? Description { get; set; }
    
    public bool Required { get; set; } = false;
    
    public bool Emphasize { get; set; } = false;
}