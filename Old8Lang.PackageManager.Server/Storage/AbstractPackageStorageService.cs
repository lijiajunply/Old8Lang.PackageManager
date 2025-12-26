using Old8Lang.PackageManager.Server.Configuration;

namespace Old8Lang.PackageManager.Server.Storage;

/// <summary>
/// 基于存储抽象层的包存储服务实现
/// </summary>
public class AbstractPackageStorageService : Services.IPackageStorageService
{
    private readonly IStorageProvider _storageProvider;
    private readonly PackageStorageOptions _options;
    private readonly ILogger<AbstractPackageStorageService> _logger;

    public AbstractPackageStorageService(
        IStorageProvider storageProvider,
        PackageStorageOptions options,
        ILogger<AbstractPackageStorageService> logger)
    {
        _storageProvider = storageProvider;
        _options = options;
        _logger = logger;
    }

    public async Task<string> StorePackageAsync(
        string packageId,
        string version,
        Stream packageStream,
        string contentType)
    {
        // 验证文件大小
        if (packageStream.Length > _options.MaxPackageSize)
        {
            throw new InvalidOperationException($"包文件大小超过限制 {_options.MaxPackageSize} 字节");
        }

        // 构建存储键
        var key = GetPackageKey(packageId, version);

        // 元数据
        var metadata = new Dictionary<string, string>
        {
            ["PackageId"] = packageId,
            ["Version"] = version,
            ["UploadedAt"] = DateTimeOffset.UtcNow.ToString("O")
        };

        try
        {
            var url = await _storageProvider.UploadAsync(key, packageStream, contentType, metadata);
            _logger.LogInformation("包已存储: {PackageId} {Version} -> {Url}", packageId, version, url);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存储包失败: {PackageId} {Version}", packageId, version);
            throw;
        }
    }

    public async Task<Stream?> GetPackageAsync(string packageId, string version)
    {
        var key = GetPackageKey(packageId, version);

        try
        {
            return await _storageProvider.DownloadAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取包失败: {PackageId} {Version}", packageId, version);
            return null;
        }
    }

    public async Task<bool> DeletePackageAsync(string packageId, string version)
    {
        var key = GetPackageKey(packageId, version);

        try
        {
            var result = await _storageProvider.DeleteAsync(key);

            if (result)
            {
                _logger.LogInformation("包已删除: {PackageId} {Version}", packageId, version);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除包失败: {PackageId} {Version}", packageId, version);
            return false;
        }
    }

    public async Task<string> CalculateChecksumAsync(string filePath)
    {
        // 如果是本地文件路径，直接计算
        if (File.Exists(filePath))
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            await using var fileStream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(fileStream);
            return Convert.ToBase64String(hash);
        }

        // 否则，尝试从存储中下载并计算
        using var stream = await _storageProvider.DownloadAsync(filePath);
        if (stream != null)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToBase64String(hash);
        }

        throw new FileNotFoundException($"文件不存在: {filePath}");
    }

    public async Task<long> GetPackageSizeAsync(string packageId, string version)
    {
        var key = GetPackageKey(packageId, version);

        try
        {
            var metadata = await _storageProvider.GetMetadataAsync(key);
            return metadata?.Size ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取包大小失败: {PackageId} {Version}", packageId, version);
            return 0;
        }
    }

    public async Task<string?> GetPackagePathAsync(string packageId, string version)
    {
        var key = GetPackageKey(packageId, version);

        try
        {
            var exists = await _storageProvider.ExistsAsync(key);
            if (!exists)
            {
                return null;
            }

            // 对于云存储，返回预签名 URL
            // 对于本地存储，FileSystemStorageProvider 会返回 file:// URL
            return await _storageProvider.GetPresignedUrlAsync(key, TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取包路径失败: {PackageId} {Version}", packageId, version);
            return null;
        }
    }

    /// <summary>
    /// 构建包的存储键（路径）
    /// </summary>
    private string GetPackageKey(string packageId, string version)
    {
        // 格式: {packageId}/{version}/{packageId}.{version}.o8pkg
        var fileName = $"{packageId}.{version}.o8pkg";
        return $"{packageId.ToLowerInvariant()}/{version}/{fileName}";
    }
}
