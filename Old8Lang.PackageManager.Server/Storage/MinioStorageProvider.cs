#if HAS_MINIO
using Minio;
using Minio.DataModel.Args;

namespace Old8Lang.PackageManager.Server.Storage;

/// <summary>
/// Minio 存储提供程序（S3 兼容）
/// </summary>
public class MinioStorageProvider : IStorageProvider
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly ILogger<MinioStorageProvider> _logger;

    public MinioStorageProvider(
        IMinioClient minioClient,
        string bucketName,
        ILogger<MinioStorageProvider> logger)
    {
        _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
        _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
        _logger = logger;

        _logger.LogInformation("Minio 存储初始化: Bucket={BucketName}", _bucketName);

        // 确保 bucket 存在
        EnsureBucketExistsAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            var exists = await _minioClient.BucketExistsAsync(beArgs);

            if (!exists)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
                _logger.LogInformation("Minio bucket 已创建: {BucketName}", _bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查/创建 Minio bucket 失败: {BucketName}", _bucketName);
        }
    }

    public async Task<string> UploadAsync(
        string key,
        Stream stream,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var putArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            if (metadata != null && metadata.Count > 0)
            {
                putArgs.WithHeaders(metadata);
            }

            await _minioClient.PutObjectAsync(putArgs, cancellationToken);
            _logger.LogDebug("文件已上传到 Minio: {Key}", key);

            return $"minio://{_bucketName}/{key}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件到 Minio 失败: {Key}", key);
            throw;
        }
    }

    public async Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var memoryStream = new MemoryStream();

            var getArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(key)
                .WithCallbackStream(async (stream, ct) =>
                {
                    await stream.CopyToAsync(memoryStream, ct);
                });

            await _minioClient.GetObjectAsync(getArgs, cancellationToken);
            memoryStream.Position = 0;

            _logger.LogDebug("文件已从 Minio 下载: {Key}", key);
            return memoryStream;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            _logger.LogWarning("Minio 文件不存在: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 Minio 下载文件失败: {Key}", key);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(key);

            await _minioClient.RemoveObjectAsync(removeArgs, cancellationToken);
            _logger.LogDebug("文件已从 Minio 删除: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 Minio 删除文件失败: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(key);

            await _minioClient.StatObjectAsync(statArgs, cancellationToken);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查 Minio 文件存在性失败: {Key}", key);
            return false;
        }
    }

    public async Task<StorageMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(key);

            var stat = await _minioClient.StatObjectAsync(statArgs, cancellationToken);

            return new StorageMetadata
            {
                Size = stat.Size,
                ContentType = stat.ContentType,
                ETag = stat.ETag,
                LastModified = stat.LastModified,
                Metadata = stat.MetaData
            };
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Minio 文件元数据失败: {Key}", key);
            return null;
        }
    }

    public async Task<IEnumerable<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();

        try
        {
            var listArgs = new ListObjectsArgs()
                .WithBucket(_bucketName)
                .WithPrefix(prefix)
                .WithRecursive(true);

            var observable = _minioClient.ListObjectsAsync(listArgs, cancellationToken);

            await foreach (var item in observable.ToAsyncEnumerable())
            {
                if (!item.IsDir)
                {
                    keys.Add(item.Key);
                }
            }

            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "列出 Minio 文件失败: Prefix={Prefix}", prefix);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var presignArgs = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(key)
                .WithExpiry((int)expiresIn.TotalSeconds);

            var url = await _minioClient.PresignedGetObjectAsync(presignArgs);
            _logger.LogDebug("生成 Minio 预签名 URL: {Key}, ExpiresIn={ExpiresIn}", key, expiresIn);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成 Minio 预签名 URL 失败: {Key}", key);
            throw;
        }
    }

    public async Task<bool> CopyAsync(
        string sourceKey,
        string destinationKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var copySourceArgs = new CopySourceObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(sourceKey);

            var copyArgs = new CopyObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(destinationKey)
                .WithCopyObjectSource(copySourceArgs);

            await _minioClient.CopyObjectAsync(copyArgs, cancellationToken);
            _logger.LogDebug("Minio 文件已复制: {SourceKey} -> {DestKey}", sourceKey, destinationKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制 Minio 文件失败: {SourceKey} -> {DestKey}", sourceKey, destinationKey);
            return false;
        }
    }
}

/// <summary>
/// IObservable 转 IAsyncEnumerable 扩展
/// </summary>
internal static class ObservableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IObservable<T> observable)
    {
        var channel = System.Threading.Channels.Channel.CreateUnbounded<T>();
        var subscription = observable.Subscribe(
            onNext: item => channel.Writer.TryWrite(item),
            onError: ex => channel.Writer.Complete(ex),
            onCompleted: () => channel.Writer.Complete()
        );

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync())
            {
                yield return item;
            }
        }
        finally
        {
            subscription.Dispose();
        }
    }
}
#endif
