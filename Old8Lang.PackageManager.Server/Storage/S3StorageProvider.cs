#if HAS_AWSSDK_S3
using Amazon.S3;
using Amazon.S3.Model;

namespace Old8Lang.PackageManager.Server.Storage;

/// <summary>
/// AWS S3 存储提供程序
/// </summary>
public class S3StorageProvider : IStorageProvider
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3StorageProvider> _logger;

    public S3StorageProvider(
        IAmazonS3 s3Client,
        string bucketName,
        ILogger<S3StorageProvider> logger)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
        _logger = logger;

        _logger.LogInformation("S3 存储初始化: Bucket={BucketName}", _bucketName);
    }

    public async Task<string> UploadAsync(
        string key,
        Stream stream,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false
        };

        if (metadata != null)
        {
            foreach (var (metaKey, metaValue) in metadata)
            {
                request.Metadata.Add(metaKey, metaValue);
            }
        }

        try
        {
            var response = await _s3Client.PutObjectAsync(request, cancellationToken);
            _logger.LogDebug("文件已上传到 S3: {Key}, ETag={ETag}", key, response.ETag);

            return $"s3://{_bucketName}/{key}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件到 S3 失败: {Key}", key);
            throw;
        }
    }

    public async Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);
            _logger.LogDebug("文件已从 S3 下载: {Key}", key);

            // 将 S3 响应流复制到内存流，以便可以多次读取
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("S3 文件不存在: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 S3 下载文件失败: {Key}", key);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);
            _logger.LogDebug("文件已从 S3 删除: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 S3 删除文件失败: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<StorageMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);

            return new StorageMetadata
            {
                Size = response.ContentLength,
                ContentType = response.Headers.ContentType,
                ETag = response.ETag,
                LastModified = response.LastModified,
                Metadata = response.Metadata.Keys.ToDictionary(k => k, k => response.Metadata[k])
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();

        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix
            };

            ListObjectsV2Response response;
            do
            {
                response = await _s3Client.ListObjectsV2Async(request, cancellationToken);
                keys.AddRange(response.S3Objects.Select(o => o.Key));

                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);

            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "列出 S3 文件失败: Prefix={Prefix}", prefix);
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
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.Add(expiresIn),
                Verb = HttpVerb.GET
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);
            _logger.LogDebug("生成 S3 预签名 URL: {Key}, ExpiresIn={ExpiresIn}", key, expiresIn);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成 S3 预签名 URL 失败: {Key}", key);
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
            var request = new CopyObjectRequest
            {
                SourceBucket = _bucketName,
                SourceKey = sourceKey,
                DestinationBucket = _bucketName,
                DestinationKey = destinationKey
            };

            await _s3Client.CopyObjectAsync(request, cancellationToken);
            _logger.LogDebug("S3 文件已复制: {SourceKey} -> {DestKey}", sourceKey, destinationKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制 S3 文件失败: {SourceKey} -> {DestKey}", sourceKey, destinationKey);
            return false;
        }
    }
}
#endif
