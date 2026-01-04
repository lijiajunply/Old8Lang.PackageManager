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
    Task<PackageEntity?> GetPackageAsync(string packageId, string version, string? language = null);
    Task<List<PackageEntity>> GetAllVersionsAsync(string packageId);
    Task<List<PackageEntity>> SearchPackagesAsync(string searchTerm, string? language = null, int skip = 0, int take = 20);
    Task<List<PackageEntity>> GetPopularPackagesAsync(string? language = null, int take = 10);
    Task<PackageEntity> UploadPackageAsync(PackageUploadRequest request, Stream packageStream);
    Task<bool> DeletePackageAsync(string packageId, string version);
    Task<bool> IncrementDownloadCountAsync(string packageId, string version);
    Task<bool> PackageExistsAsync(string packageId, string version, string? language = null);
}

/// <summary>
/// 包管理服务实现
/// </summary>
public class PackageManagementService(
    PackageManagerDbContext dbContext,
    IPackageStorageService storageService,
    IPackageSignatureService signatureService,
    IPackageQualityService qualityService,
    ILogger<PackageManagementService> logger,
    IPythonPackageParser pythonParser)
    : IPackageManagementService
{
    public async Task<PackageEntity?> GetPackageAsync(string packageId, string version, string? language = null)
    {
        var query = dbContext.Packages
            .Include(p => p.PackageTags)
            .Include(p => p.PackageDependencies)
            .Include(p => p.Files)
            .Include(p => p.ExternalDependencies)
            .Include(p => p.QualityScore)
            .Where(p => p.PackageId == packageId && p.Version == version);
            
        if (!string.IsNullOrEmpty(language))
        {
            query = query.Where(p => p.Language == language);
        }
        
        return await query.FirstOrDefaultAsync();
    }
    
    public async Task<List<PackageEntity>> GetAllVersionsAsync(string packageId)
    {
        return await dbContext.Packages
            .Include(p => p.PackageTags)
            .Include(p => p.PackageDependencies)
            .Where(p => p.PackageId == packageId)
            .OrderByDescending(p => p.PublishedAt)
            .ToListAsync();
    }
    
    public async Task<List<PackageEntity>> SearchPackagesAsync(string searchTerm, string? language = null, int skip = 0, int take = 20)
    {
        var query = dbContext.Packages
            .Include(p => p.PackageTags)
            .Include(p => p.PackageDependencies)
            .Include(p => p.Files)
            .Include(p => p.ExternalDependencies)
            .Include(p => p.QualityScore)
            .Where(p => p.IsListed);
        
        // 语言筛选
        if (!string.IsNullOrEmpty(language))
        {
            query = query.Where(p => p.Language == language.ToLowerInvariant());
        }
        
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
    
    public async Task<List<PackageEntity>> GetPopularPackagesAsync(string? language = null, int take = 10)
    {
        var query = dbContext.Packages
            .Include(p => p.PackageTags)
            .Include(p => p.PackageDependencies)
            .Include(p => p.ExternalDependencies)
            .Include(p => p.QualityScore)
            .Where(p => p.IsListed && !p.IsPrerelease);
        
        // 语言筛选
        if (!string.IsNullOrEmpty(language))
        {
            query = query.Where(p => p.Language == language.ToLowerInvariant());
        }
        
        return await query
            .OrderByDescending(p => p.DownloadCount)
            .ThenByDescending(p => p.PublishedAt)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<PackageEntity> UploadPackageAsync(PackageUploadRequest request, Stream packageStream)
    {
        // 解析包信息
        var packageInfo = await ExtractPackageInfoAsync(packageStream, request.PackageFile.FileName);
        if (packageInfo == null)
        {
            throw new InvalidOperationException("无法解析包信息");
        }

        // 检查包是否已存在
        var existingPackage = await GetPackageAsync(packageInfo.Id, packageInfo.Version, request.Language);
        if (existingPackage != null)
        {
            throw new InvalidOperationException($"包 {packageInfo.Id} 版本 {packageInfo.Version} (语言: {request.Language}) 已存在");
        }

        // 重置流位置
        packageStream.Position = 0;

        // 存储包文件
        var packageFilePath = await storageService.StorePackageAsync(packageInfo.Id, packageInfo.Version, packageStream, "application/octet-stream");

        // 验证包签名 (如果已启用)
        var signatureVerificationResult = await signatureService.VerifyPackageSignatureAsync(packageFilePath);
        if (!signatureVerificationResult.IsValid)
        {
            logger.LogWarning("包签名验证失败: {PackageId} {Version}, 原因: {Message}",
                packageInfo.Id, packageInfo.Version, signatureVerificationResult.Message);
            // 注意: 这里不抛出异常，因为签名可能未启用或是可选的
            // 如果需要强制签名验证，可以在这里抛出异常
        }

        // 计算校验和
        var checksum = await storageService.CalculateChecksumAsync(packageFilePath);

        // 创建包实体
        var packageEntity = new PackageEntity
        {
            PackageId = packageInfo.Id,
            Version = packageInfo.Version,
            Language = request.Language.ToLowerInvariant(),
            Description = request.Description ?? packageInfo.Description,
            Author = request.Author ?? packageInfo.Author,
            License = request.License,
            ProjectUrl = request.ProjectUrl,
            Checksum = checksum,
            Size = await storageService.GetPackageSizeAsync(packageInfo.Id, packageInfo.Version),
            PublishedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DownloadCount = 0,
            IsListed = true,
            IsPrerelease = request.IsPrerelease || IsPrereleaseVersion(packageInfo.Version),
            IsSigned = signatureVerificationResult.IsValid,
            SignedBy = signatureVerificationResult.Signature?.Signer.Name ?? signatureVerificationResult.Signature?.Signer.Email,
            SignedAt = signatureVerificationResult.Signature?.Timestamp
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
        
        // 添加外部依赖（用于 Python 包等）
        foreach (var externalDep in request.ExternalDependencies)
        {
            packageEntity.ExternalDependencies.Add(new ExternalDependencyEntity
            {
                DependencyType = externalDep.DependencyType,
                PackageName = externalDep.PackageName,
                VersionSpec = externalDep.VersionSpec,
                IndexUrl = externalDep.IndexUrl,
                ExtraIndexUrl = externalDep.ExtraIndexUrl,
                IsDevDependency = externalDep.IsDevDependency
            });
        }
        
        // 添加语言特定元数据
        if (!string.IsNullOrEmpty(request.LanguageMetadata))
        {
            packageEntity.LanguageMetadata.Add(new LanguageMetadataEntity
            {
                Language = request.Language.ToLowerInvariant(),
                Metadata = request.LanguageMetadata,
                UpdatedAt = DateTime.UtcNow
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
        dbContext.Packages.Add(packageEntity);
        await dbContext.SaveChangesAsync();

        // Calculate and save quality score
        try
        {
            var qualityScore = await qualityService.CalculateQualityScoreAsync(packageEntity);
            dbContext.PackageQualityScores.Add(qualityScore);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Quality score calculated for package {PackageId} {Version}: {Score}",
                packageInfo.Id, packageInfo.Version, qualityScore.QualityScore);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to calculate quality score for package {PackageId} {Version}",
                packageInfo.Id, packageInfo.Version);
            // Don't fail the upload if quality score calculation fails
        }

        logger.LogInformation("包上传成功: {PackageId} {Version}", packageInfo.Id, packageInfo.Version);

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
        await storageService.DeletePackageAsync(packageId, version);
        
        // 删除数据库记录
        dbContext.Packages.Remove(package);
        await dbContext.SaveChangesAsync();
        
        logger.LogInformation("包删除成功: {PackageId} {Version}", packageId, version);
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
        await dbContext.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<bool> PackageExistsAsync(string packageId, string version, string? language = null)
    {
        var query = dbContext.Packages
            .Where(p => p.PackageId == packageId && p.Version == version);
            
        if (!string.IsNullOrEmpty(language))
        {
            query = query.Where(p => p.Language == language);
        }
        
        return await query.AnyAsync();
    }
    
    private async Task<Package?> ExtractPackageInfoAsync(Stream packageStream, string fileName)
    {
        try
        {
            // 根据文件扩展名判断语言
            var language = DeterminePackageLanguage(fileName);
            
            switch (language)
            {
                case "old8lang":
                    return await ExtractOld8LangPackageInfoAsync(packageStream, fileName);
                case "python":
                    return await ExtractPythonPackageInfoAsync(packageStream, fileName);
                default:
                    logger.LogWarning("不支持的包格式: {FileName}", fileName);
                    return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "解析包信息失败: {FileName}", fileName);
            return null;
        }
    }
    
    private async Task<Package?> ExtractOld8LangPackageInfoAsync(Stream packageStream, string fileName)
    {
        // 这里应该实现解压 .o8pkg 文件并读取 package.json 的逻辑
        // 为简化示例，返回默认的包信息
        
        packageStream.Position = 0;
        // Use leaveOpen: true to prevent StreamReader from closing the underlying stream
        using var reader = new StreamReader(packageStream, leaveOpen: true);
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
    
    private async Task<Package?> ExtractPythonPackageInfoAsync(Stream packageStream, string fileName)
    {
        var pythonInfo = await pythonParser.ParsePackageAsync(packageStream, fileName);
        
        if (pythonInfo == null)
            return null;
        
        return new Package
        {
            Id = pythonInfo.PackageId,
            Version = pythonInfo.Version,
            Description = pythonInfo.Summary,
            Author = pythonInfo.Author,
            Tags = pythonInfo.Keywords,
            Dependencies = pythonInfo.Dependencies.Select(d => new PackageDependency
            {
                PackageId = d.PackageName,
                VersionRange = d.VersionSpec,
                IsRequired = true
            }).ToList()
        };
    }
    
    private string DeterminePackageLanguage(string fileName)
    {
        var lowerFileName = fileName.ToLowerInvariant();
        
        if (lowerFileName.EndsWith(".o8pkg"))
            return "old8lang";
        
        if (lowerFileName.EndsWith(".whl") || lowerFileName.EndsWith(".tar.gz"))
            return "python";
        
        return "unknown";
    }
    
    private static bool IsPrereleaseVersion(string version)
    {
        return version.Contains('-') || version.Contains("alpha") || version.Contains("beta") || version.Contains("rc");
    }
}