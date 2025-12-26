namespace Old8Lang.PackageManager.Server.Storage;

/// <summary>
/// 存储提供程序接口
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="key">存储键（路径）</param>
    /// <param name="stream">文件流</param>
    /// <param name="contentType">内容类型</param>
    /// <param name="metadata">元数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储的文件 URL</returns>
    Task<string> UploadAsync(
        string key,
        Stream stream,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="key">存储键（路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件流，如果不存在则返回 null</returns>
    Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="key">存储键（路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="key">存储键（路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件是否存在</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件元数据
    /// </summary>
    /// <param name="key">存储键（路径）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件元数据，如果不存在则返回 null</returns>
    Task<StorageMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出指定前缀下的所有文件
    /// </summary>
    /// <param name="prefix">前缀</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件键列表</returns>
    Task<IEnumerable<string>> ListAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件的预签名 URL（用于直接下载）
    /// </summary>
    /// <param name="key">存储键（路径）</param>
    /// <param name="expiresIn">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预签名 URL</returns>
    Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 复制文件
    /// </summary>
    /// <param name="sourceKey">源文件键</param>
    /// <param name="destinationKey">目标文件键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否复制成功</returns>
    Task<bool> CopyAsync(
        string sourceKey,
        string destinationKey,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 存储元数据
/// </summary>
public class StorageMetadata
{
    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// ETag（用于缓存验证）
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// 自定义元数据
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// 存储提供程序类型
/// </summary>
public enum StorageProviderType
{
    /// <summary>
    /// 本地文件系统
    /// </summary>
    FileSystem,

    /// <summary>
    /// AWS S3
    /// </summary>
    S3,

    /// <summary>
    /// Azure Blob Storage
    /// </summary>
    AzureBlob,

    /// <summary>
    /// Minio（S3 兼容）
    /// </summary>
    Minio
}
