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
    
    public DbSet<PackageEntity> Packages { get; set; }
    public DbSet<PackageTagEntity> PackageTags { get; set; }
    public DbSet<PackageDependencyEntity> PackageDependencies { get; set; }
    public DbSet<PackageFileEntity> PackageFiles { get; set; }
    public DbSet<ApiKeyEntity> ApiKeys { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // PackageEntity 配置
        modelBuilder.Entity<PackageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PackageId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.PackageId, e.Version }).IsUnique();
            entity.HasIndex(e => e.PackageId);
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