using Microsoft.EntityFrameworkCore;
using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Server.Data;
using Old8Lang.PackageManager.Server.Models;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// 包管理服务
/// </summary>
public interface IPackageManagementService
{
    Task<PackageEntity?> GetPackageAsync(string packageId, string version);
    Task<List<PackageEntity>> GetAllVersionsAsync(string packageId);
    Task<List<PackageEntity>> SearchPackagesAsync(string searchTerm, int skip = 0, int take = 20);
    Task<List<PackageEntity>> GetPopularPackagesAsync(int take = 10);
    Task<PackageEntity> UploadPackageAsync(PackageUploadRequest request, Stream packageStream);
    Task<bool> DeletePackageAsync(string packageId, string version);
    Task<bool> IncrementDownloadCountAsync(string packageId, string version);
    Task<bool> PackageExistsAsync(string packageId, string version);
}

/// <summary>
/// 包管理服务实现
/// </summary>
public class PackageManagementService : IPackageManagementService
{
    private readonly PackageManagerDbContext _dbContext;
    private readonly IPackageStorageService _storageService;
    private readonly ILogger<PackageManagementService> _logger;
    
    public PackageManagementService(
        PackageManagerDbContext dbContext,
        IPackageStorageService storageService,
        ILogger<PackageManagementService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _logger = logger;
    }
    
    public async Task<PackageEntity?> GetPackageAsync(string packageId, string version)
    {
        return await _dbContext.Packages
            .Include(p => p.PackageTags)
            .Include(p => p.PackageDependencies)
            .Include(p => p.Files)
            .FirstOrDefaultAsync(p => p.PackageId == packageId && p.Version == version);
    }
    
    public async Task<List<PackageEntity>> GetAllVersionsAsync(string packageId)
    {
        return await _dbContext.Packages
            .Include(p => p.PackageTags)
            .Include(p => p.PackageDependencies)
            .Where(p => p.PackageId == packageId)
            .OrderByDescending(p => p.PublishedAt)
            .ToListAsync();
    }
    
    public async Task<List<PackageEntity>> SearchPackagesAsync(string searchTerm, int skip = 0, int take = 20)
    {
        var query = _dbContext.Packages
            .Include(p => p.PackageTags)
            .Include(p => p.PackageDependencies)
            .Where(p => p.IsListed);
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLowerInvariant();
            query = query.Where(p => 
                p.PackageId.ToLower().Contains(searchTerm) ||
                p.Description.ToLower().Contains(searchTerm) ||
                p.PackageTags.Any(t => t.Tag.ToLower().Contains(searchTerm)));
        }
        
        return await query
            .OrderByDescending(p => p.DownloadCount)
            .ThenByDescending(p => p.PublishedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<List<PackageEntity>> GetPopularPackagesAsync(int take = 10)
    {
        return await _dbContext.Packages
            .Include(p => p.PackageTags)
            .Include(p => p.PackageDependencies)
            .Where(p => p.IsListed && !p.IsPrerelease)
            .OrderByDescending(p => p.DownloadCount)
            .ThenByDescending(p => p.PublishedAt)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<PackageEntity> UploadPackageAsync(PackageUploadRequest request, Stream packageStream)
    {
        // 解析包信息
        var packageInfo = await ExtractPackageInfoAsync(packageStream);
        if (packageInfo == null)
        {
            throw new InvalidOperationException("无法解析包信息");
        }
        
        // 检查包是否已存在
        var existingPackage = await GetPackageAsync(packageInfo.Id, packageInfo.Version);
        if (existingPackage != null)
        {
            throw new InvalidOperationException($"包 {packageInfo.Id} 版本 {packageInfo.Version} 已存在");
        }
        
        // 重置流位置
        packageStream.Position = 0;
        
        // 存储包文件
        var packageFilePath = await _storageService.StorePackageAsync(packageInfo.Id, packageInfo.Version, packageStream, "application/octet-stream");
        
        // 计算校验和
        var checksum = await _storageService.CalculateChecksumAsync(packageFilePath);
        
        // 创建包实体
        var packageEntity = new PackageEntity
        {
            PackageId = packageInfo.Id,
            Version = packageInfo.Version,
            Description = request.Description ?? packageInfo.Description,
            Author = request.Author ?? packageInfo.Author,
            License = request.License,
            ProjectUrl = request.ProjectUrl,
            Checksum = checksum,
            Size = await _storageService.GetPackageSizeAsync(packageInfo.Id, packageInfo.Version),
            PublishedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DownloadCount = 0,
            IsListed = true,
            IsPrerelease = request.IsPrerelease || IsPrereleaseVersion(packageInfo.Version)
        };
        
        // 添加标签
        var tags = request.Tags.Any() ? request.Tags : packageInfo.Tags;
        foreach (var tag in tags.Distinct())
        {
            packageEntity.PackageTags.Add(new PackageTagEntity
            {
                Tag = tag
            });
        }
        
        // 添加依赖
        var dependencies = packageInfo.Dependencies ?? new List<PackageDependency>();
        foreach (var dependency in dependencies)
        {
            packageEntity.PackageDependencies.Add(new PackageDependencyEntity
            {
                DependencyId = dependency.PackageId,
                VersionRange = dependency.VersionRange,
                IsRequired = dependency.IsRequired,
                TargetFramework = ""
            });
        }
        
        // 添加文件信息
        packageEntity.Files.Add(new PackageFileEntity
        {
            FileName = $"{packageInfo.Id}.{packageInfo.Version}.o8pkg",
            FilePath = packageFilePath,
            FileSize = packageEntity.Size,
            ContentType = "application/octet-stream",
            Checksum = checksum
        });
        
        // 保存到数据库
        _dbContext.Packages.Add(packageEntity);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("包上传成功: {PackageId} {Version}", packageInfo.Id, packageInfo.Version);
        
        return packageEntity;
    }
    
    public async Task<bool> DeletePackageAsync(string packageId, string version)
    {
        var package = await GetPackageAsync(packageId, version);
        if (package == null)
        {
            return false;
        }
        
        // 删除存储的包文件
        await _storageService.DeletePackageAsync(packageId, version);
        
        // 删除数据库记录
        _dbContext.Packages.Remove(package);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("包删除成功: {PackageId} {Version}", packageId, version);
        return true;
    }
    
    public async Task<bool> IncrementDownloadCountAsync(string packageId, string version)
    {
        var package = await GetPackageAsync(packageId, version);
        if (package == null)
        {
            return false;
        }
        
        package.DownloadCount++;
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<bool> PackageExistsAsync(string packageId, string version)
    {
        return await _dbContext.Packages
            .AnyAsync(p => p.PackageId == packageId && p.Version == version);
    }
    
    private async Task<Package?> ExtractPackageInfoAsync(Stream packageStream)
    {
        try
        {
            // 这里应该实现解压 .o8pkg 文件并读取 package.json 的逻辑
            // 为简化示例，返回默认的包信息
            
            // 临时实现：假设从文件名解析包信息
            packageStream.Position = 0;
            using var reader = new StreamReader(packageStream);
            var firstLine = await reader.ReadLineAsync();
            packageStream.Position = 0;
            
            // 实际实现应该使用 ZIP 库解压并读取 package.json
            return new Package
            {
                Id = "UnknownPackage",
                Version = "1.0.0",
                Description = "",
                Author = "",
                Tags = new List<string>(),
                Dependencies = new List<PackageDependency>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析包信息失败");
            return null;
        }
    }
    
    private static bool IsPrereleaseVersion(string version)
    {
        return version.Contains('-') || version.Contains("alpha") || version.Contains("beta") || version.Contains("rc");
    }
}