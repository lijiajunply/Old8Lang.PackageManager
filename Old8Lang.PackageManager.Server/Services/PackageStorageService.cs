using Microsoft.EntityFrameworkCore;
using Old8Lang.PackageManager.Server.Configuration;
using Old8Lang.PackageManager.Server.Data;
using Old8Lang.PackageManager.Server.Models;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// 包存储服务
/// </summary>
public interface IPackageStorageService
{
    Task<string> StorePackageAsync(string packageId, string version, Stream packageStream, string contentType);
    Task<Stream?> GetPackageAsync(string packageId, string version);
    Task<bool> DeletePackageAsync(string packageId, string version);
    Task<string> CalculateChecksumAsync(string filePath);
    Task<long> GetPackageSizeAsync(string packageId, string version);
}

/// <summary>
/// 包存储服务实现
/// </summary>
public class PackageStorageService : IPackageStorageService
{
    private readonly PackageStorageOptions _options;
    private readonly ILogger<PackageStorageService> _logger;
    
    public PackageStorageService(PackageStorageOptions options, ILogger<PackageStorageService> logger)
    {
        _options = options;
        _logger = logger;
        
        // 确保存储目录存在
        Directory.CreateDirectory(_options.StoragePath);
    }
    
    public async Task<string> StorePackageAsync(string packageId, string version, Stream packageStream, string contentType)
    {
        // 验证文件大小
        if (packageStream.Length > _options.MaxPackageSize)
        {
            throw new InvalidOperationException($"包文件大小超过限制 {_options.MaxPackageSize} 字节");
        }
        
        // 创建包目录
        var packageDir = Path.Combine(_options.StoragePath, packageId.ToLowerInvariant(), version);
        Directory.CreateDirectory(packageDir);
        
        // 存储包文件
        var packageFileName = $"{packageId}.{version}.o8pkg";
        var packageFilePath = Path.Combine(packageDir, packageFileName);
        
        await using var fileStream = new FileStream(packageFilePath, FileMode.Create, FileAccess.Write);
        await packageStream.CopyToAsync(fileStream);
        
        _logger.LogInformation("包已存储: {PackageId} {Version} -> {FilePath}", packageId, version, packageFilePath);
        
        return packageFilePath;
    }
    
    public async Task<Stream?> GetPackageAsync(string packageId, string version)
    {
        var packageDir = Path.Combine(_options.StoragePath, packageId.ToLowerInvariant(), version);
        var packageFileName = $"{packageId}.{version}.o8pkg";
        var packageFilePath = Path.Combine(packageDir, packageFileName);
        
        if (!File.Exists(packageFilePath))
        {
            return null;
        }
        
        return new FileStream(packageFilePath, FileMode.Open, FileAccess.Read);
    }
    
    public async Task<bool> DeletePackageAsync(string packageId, string version)
    {
        var packageDir = Path.Combine(_options.StoragePath, packageId.ToLowerInvariant(), version);
        
        if (!Directory.Exists(packageDir))
        {
            return false;
        }
        
        try
        {
            Directory.Delete(packageDir, true);
            
            // 如果包目录为空，删除它
            var parentDir = Path.Combine(_options.StoragePath, packageId.ToLowerInvariant());
            if (Directory.Exists(parentDir) && !Directory.GetFileSystemEntries(parentDir).Any())
            {
                Directory.Delete(parentDir);
            }
            
            _logger.LogInformation("包已删除: {PackageId} {Version}", packageId, version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除包失败: {PackageId} {Version}", packageId, version);
            return false;
        }
    }
    
    public async Task<string> CalculateChecksumAsync(string filePath)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        await using var fileStream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(fileStream);
        return Convert.ToBase64String(hash);
    }
    
    public async Task<long> GetPackageSizeAsync(string packageId, string version)
    {
        var packageDir = Path.Combine(_options.StoragePath, packageId.ToLowerInvariant(), version);
        var packageFileName = $"{packageId}.{version}.o8pkg";
        var packageFilePath = Path.Combine(packageDir, packageFileName);
        
        if (!File.Exists(packageFilePath))
        {
            return 0;
        }
        
        var fileInfo = new FileInfo(packageFilePath);
        return fileInfo.Length;
    }
}