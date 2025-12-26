#if HAS_AZURE_STORAGE_BLOBS
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Old8Lang.PackageManager.Server.Storage;

/// <summary>
/// Azure Blob Storage 存储提供程序
/// </summary>
public class AzureBlobStorageProvider : IStorageProvider
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageProvider> _logger;

    public AzureBlobStorageProvider(
        BlobContainerClient containerClient,
        ILogger<AzureBlobStorageProvider> logger)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
        _logger = logger;

        _logger.LogInformation("Azure Blob 存储初始化: Container={ContainerName}", _containerClient.Name);

        // 确保容器存在
        _containerClient.CreateIfNotExists();
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
            var blobClient = _containerClient.GetBlobClient(key);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata != null ? new Dictionary<string, string>(metadata) : null
            };

            await blobClient.UploadAsync(stream, options, cancellationToken);
            _logger.LogDebug("文件已上传到 Azure Blob: {Key}", key);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件到 Azure Blob 失败: {Key}", key);
            throw;
        }
    }

    public async Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(key);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Azure Blob 文件不存在: {Key}", key);
                return null;
            }

            var response = await blobClient.DownloadAsync(cancellationToken);
            var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            _logger.LogDebug("文件已从 Azure Blob 下载: {Key}", key);
            return memoryStream;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Azure Blob 文件不存在: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 Azure Blob 下载文件失败: {Key}", key);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(key);
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            if (response.Value)
            {
                _logger.LogDebug("文件已从 Azure Blob 删除: {Key}", key);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 Azure Blob 删除文件失败: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(key);
            return await blobClient.ExistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查 Azure Blob 文件存在性失败: {Key}", key);
            return false;
        }
    }

    public async Task<StorageMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(key);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            return new StorageMetadata
            {
                Size = properties.Value.ContentLength,
                ContentType = properties.Value.ContentType,
                ETag = properties.Value.ETag.ToString(),
                LastModified = properties.Value.LastModified,
                Metadata = properties.Value.Metadata
            };
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Azure Blob 文件元数据失败: {Key}", key);
            return null;
        }
    }

    public async Task<IEnumerable<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();

        try
        {
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                keys.Add(blobItem.Name);
            }

            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "列出 Azure Blob 文件失败: Prefix={Prefix}", prefix);
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
            var blobClient = _containerClient.GetBlobClient(key);

            // 检查容器是否支持生成 SAS
            if (!_containerClient.CanGenerateSasUri)
            {
                // 如果不能生成 SAS，返回直接 URL
                _logger.LogWarning("容器不支持生成 SAS URL，返回直接 URL: {Key}", key);
                return blobClient.Uri.ToString();
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = key,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            _logger.LogDebug("生成 Azure Blob SAS URL: {Key}, ExpiresIn={ExpiresIn}", key, expiresIn);

            return sasUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成 Azure Blob SAS URL 失败: {Key}", key);
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
            var sourceBlobClient = _containerClient.GetBlobClient(sourceKey);
            var destBlobClient = _containerClient.GetBlobClient(destinationKey);

            var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: cancellationToken);
            await copyOperation.WaitForCompletionAsync(cancellationToken);

            _logger.LogDebug("Azure Blob 文件已复制: {SourceKey} -> {DestKey}", sourceKey, destinationKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制 Azure Blob 文件失败: {SourceKey} -> {DestKey}", sourceKey, destinationKey);
            return false;
        }
    }
}
#endif
