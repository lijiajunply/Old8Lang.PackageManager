using Microsoft.EntityFrameworkCore;
using Old8Lang.PackageManager.Server.Models;

namespace Old8Lang.PackageManager.Server.Data;

/// <summary>
/// 包管理器数据库上下文
/// </summary>
public class PackageManagerDbContext : DbContext
{
    public PackageManagerDbContext(DbContextOptions<PackageManagerDbContext> options) : base(options)
    {
    }
    
    public virtual DbSet<PackageEntity> Packages { get; set; } = null!;
    public virtual DbSet<PackageTagEntity> PackageTags { get; set; } = null!;
    public virtual DbSet<PackageDependencyEntity> PackageDependencies { get; set; } = null!;
    public virtual DbSet<PackageFileEntity> PackageFiles { get; set; } = null!;
    public virtual DbSet<ApiKeyEntity> ApiKeys { get; set; } = null!;
    public virtual DbSet<ExternalDependencyEntity> ExternalDependencies { get; set; } = null!;
    public virtual DbSet<LanguageMetadataEntity> LanguageMetadata { get; set; } = null!;
    
    // 用户相关表
    public virtual DbSet<UserEntity> Users { get; set; } = null!;
    public virtual DbSet<UserExternalLoginEntity> UserExternalLogins { get; set; } = null!;
    public virtual DbSet<RefreshTokenEntity> RefreshTokens { get; set; } = null!;
    public virtual DbSet<UserSessionEntity> UserSessions { get; set; } = null!;
    public virtual DbSet<UserRoleEntity> UserRoles { get; set; } = null!;
    public virtual DbSet<UserRoleMappingEntity> UserRoleMappings { get; set; } = null!;
    public virtual DbSet<UserActivityLogEntity> UserActivityLogs { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // PackageEntity 配置
        modelBuilder.Entity<PackageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PackageId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(20).HasDefaultValue("old8lang");
            entity.HasIndex(e => new { e.PackageId, e.Version, e.Language }).IsUnique();
            entity.HasIndex(e => e.PackageId);
            entity.HasIndex(e => e.Language);
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => e.DownloadCount);
        });
        
        // PackageTagEntity 配置
        modelBuilder.Entity<PackageTagEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tag).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Package)
                  .WithMany(p => p.PackageTags)
                  .HasForeignKey(e => e.PackageEntityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // PackageDependencyEntity 配置
        modelBuilder.Entity<PackageDependencyEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DependencyId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.VersionRange).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Package)
                  .WithMany(p => p.PackageDependencies)
                  .HasForeignKey(e => e.PackageEntityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // PackageFileEntity 配置
        modelBuilder.Entity<PackageFileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Package)
                  .WithMany(p => p.Files)
                  .HasForeignKey(e => e.PackageEntityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // ApiKeyEntity 配置
        modelBuilder.Entity<ApiKeyEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });
        
        // ExternalDependencyEntity 配置
        modelBuilder.Entity<ExternalDependencyEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DependencyType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PackageName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.VersionSpec).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IndexUrl).HasMaxLength(50);
            entity.Property(e => e.ExtraIndexUrl).HasMaxLength(50);
            
            entity.HasOne(e => e.Package)
                  .WithMany(p => p.ExternalDependencies)
                  .HasForeignKey(e => e.PackageEntityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // LanguageMetadataEntity 配置
        modelBuilder.Entity<LanguageMetadataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Metadata).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.Package)
                  .WithMany()
                  .HasForeignKey(e => e.PackageEntityId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.PackageEntityId, e.Language }).IsUnique();
        });
        
        // UserEntity 配置
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(500);
            entity.Property(e => e.Company).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.SubjectId).HasMaxLength(100);
            entity.Property(e => e.Provider).HasMaxLength(100);
            entity.Property(e => e.ProviderDisplayName).HasMaxLength(500);
            entity.Property(e => e.PreferredLanguage).HasMaxLength(50);
            
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.SubjectId).IsUnique();
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.CreatedAt);
        });
        
        // UserExternalLoginEntity 配置
        modelBuilder.Entity<UserExternalLoginEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProviderKey).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ProviderDisplayName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.SubjectId).HasMaxLength(100);
            entity.Property(e => e.ProviderData).HasMaxLength(1000);
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ExternalLogins)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();
        });
        
        // RefreshTokenEntity 配置
        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.JwtId).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RevokedReason).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(200);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.JwtId).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });
        
        // UserSessionEntity 配置
        modelBuilder.Entity<UserSessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserSessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });
        
        // UserRoleEntity 配置
        modelBuilder.Entity<UserRoleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            
            entity.HasIndex(e => e.RoleName).IsUnique();
        });
        
        // UserRoleMappingEntity 配置
        modelBuilder.Entity<UserRoleMappingEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserMappings)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.AssignedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        
        // UserActivityLogEntity 配置
        modelBuilder.Entity<UserActivityLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(200);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Metadata).HasMaxLength(2000);
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ActivityType);
            entity.HasIndex(e => e.CreatedAt);
        });
        
        // 数据种子
        SeedData(modelBuilder);
    }
    
    private static void SeedData(ModelBuilder modelBuilder)
    {
        // 创建默认 API 密钥
        var apiKey = new ApiKeyEntity
        {
            Id = 1,
            Name = "Development API Key",
            Key = GenerateApiKey(),
            Description = "开发环境使用的默认 API 密钥",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            IsActive = true,
            Scopes = "package:read,package:write,admin:all",
            UsageCount = 0
        };
        
        modelBuilder.Entity<ApiKeyEntity>().HasData(apiKey);
    }
    
    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-");
    }
}