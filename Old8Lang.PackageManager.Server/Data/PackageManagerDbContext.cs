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
    
    public virtual DbSet<PackageEntity> Packages { get; set; }
    public virtual DbSet<PackageTagEntity> PackageTags { get; set; }
    public virtual DbSet<PackageDependencyEntity> PackageDependencies { get; set; }
    public virtual DbSet<PackageFileEntity> PackageFiles { get; set; }
    public virtual DbSet<ApiKeyEntity> ApiKeys { get; set; }
    public virtual DbSet<ExternalDependencyEntity> ExternalDependencies { get; set; }
    public virtual DbSet<LanguageMetadataEntity> LanguageMetadata { get; set; }
    
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