using System.ComponentModel.DataAnnotations;

namespace Old8Lang.PackageManager.Server.Models;

/// <summary>
/// 用户实体
/// </summary>
public class UserEntity
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(500)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? PasswordHash { get; set; }
    
    [MaxLength(200)]
    public string? DisplayName { get; set; }
    
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
    
    [MaxLength(1000)]
    public string? Bio { get; set; }
    
    [Url]
    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }
    
    [MaxLength(500)]
    public string? Company { get; set; }
    
    [MaxLength(200)]
    public string? Location { get; set; }
    
    public bool IsEmailVerified { get; set; } = false;
    
    public DateTime EmailVerifiedAt { get; set; } = DateTime.MinValue;
    
    public bool IsActive { get; set; } = true;
    
    public bool IsAdmin { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastLoginAt { get; set; } = DateTime.MinValue;
    
    // 统计信息
    public int PackageCount { get; set; } = 0;
    
    public long TotalDownloads { get; set; } = 0;
    
    // 包容量统计
    public long UsedStorage { get; set; } = 0;
    
    // 用户偏好设置
    [MaxLength(50)]
    public string? PreferredLanguage { get; set; } = "zh-CN";
    
    public bool EmailNotificationsEnabled { get; set; } = true;
    
    public bool ReceiveSecurityAlerts { get; set; } = true;
    
    // OIDC 登录信息
    [MaxLength(100)]
    public string? SubjectId { get; set; }
    
    [MaxLength(100)]
    public string? Provider { get; set; }
    
    [MaxLength(500)]
    public string? ProviderDisplayName { get; set; }
    
    // 外部登录关联
    public virtual ICollection<UserExternalLoginEntity> ExternalLogins { get; set; } = new List<UserExternalLoginEntity>();
    
    public virtual ICollection<RefreshTokenEntity> RefreshTokens { get; set; } = new List<RefreshTokenEntity>();
    
    public virtual ICollection<UserSessionEntity> UserSessions { get; set; } = new List<UserSessionEntity>();
    
    public virtual ICollection<PackageEntity> Packages { get; set; } = new List<PackageEntity>();
}

/// <summary>
/// 外部登录实体
/// </summary>
public class UserExternalLoginEntity
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string ProviderKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string ProviderDisplayName { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? SubjectId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(1000)]
    public string? ProviderData { get; set; }
    
    // 外键
    public int UserId { get; set; }
    
    // 导航属性
    public virtual UserEntity User { get; set; } = null!;
}

/// <summary>
/// 刷新令牌实体
/// </summary>
public class RefreshTokenEntity
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string JwtId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsUsed { get; set; } = false;
    
    public bool IsRevoked { get; set; } = false;
    
    public DateTime? RevokedAt { get; set; }
    
    [MaxLength(100)]
    public string? RevokedReason { get; set; }
    
    [MaxLength(200)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    // 外键
    public int UserId { get; set; }
    
    // 导航属性
    public virtual UserEntity User { get; set; } = null!;
}

/// <summary>
/// 用户会话实体
/// </summary>
public class UserSessionEntity
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string SessionId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int RequestCount { get; set; } = 0;
    
    // 外键
    public int UserId { get; set; }
    
    // 导航属性
    public virtual UserEntity User { get; set; } = null!;
}

/// <summary>
/// 用户角色实体
/// </summary>
public class UserRoleEntity
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    public virtual ICollection<UserRoleMappingEntity> UserMappings { get; set; } = new List<UserRoleMappingEntity>();
}

/// <summary>
/// 用户角色映射实体
/// </summary>
public class UserRoleMappingEntity
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public int RoleId { get; set; }
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    public int AssignedByUserId { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    // 导航属性
    public virtual UserEntity User { get; set; } = null!;
    
    public virtual UserRoleEntity Role { get; set; } = null!;
    
    public virtual UserEntity AssignedByUser { get; set; } = null!;
}

/// <summary>
/// 用户活动日志实体
/// </summary>
public class UserActivityLogEntity
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ActivityType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    public string? Metadata { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    public virtual UserEntity User { get; set; } = null!;
}