namespace Old8Lang.PackageManager.Server.Storage;

/// <summary>
/// 本地文件系统存储提供程序
/// </summary>
public class FileSystemStorageProvider : IStorageProvider
{
    private readonly string _rootPath;
    private readonly ILogger<FileSystemStorageProvider> _logger;

    public FileSystemStorageProvider(string rootPath, ILogger<FileSystemStorageProvider> logger)
    {
        _rootPath = Path.GetFullPath(rootPath);
        _logger = logger;

        // 确保根目录存在
        Directory.CreateDirectory(_rootPath);

        _logger.LogInformation("文件系统存储初始化: {RootPath}", _rootPath);
    }

    public async Task<string> UploadAsync(
        string key,
        Stream stream,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetFullPath(key);
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, cancellationToken);

        // 保存元数据到伴随文件
        if (metadata != null && metadata.Count > 0)
        {
            await SaveMetadataAsync(filePath, contentType, metadata, cancellationToken);
        }

        _logger.LogDebug("文件已上传: {Key} -> {FilePath}", key, filePath);
        return filePath;
    }

    public async Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetFullPath(key);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("文件不存在: {Key}", key);
            return null;
        }

        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetFullPath(key);

        if (!File.Exists(filePath))
        {
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(filePath);

            // 删除元数据文件
            var metadataPath = GetMetadataPath(filePath);
            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }

            // 尝试删除空目录
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }

            _logger.LogDebug("文件已删除: {Key}", key);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除文件失败: {Key}", key);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetFullPath(key);
        return Task.FromResult(File.Exists(filePath));
    }

    public async Task<StorageMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetFullPath(key);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var fileInfo = new FileInfo(filePath);
        var metadata = new StorageMetadata
        {
            Size = fileInfo.Length,
            LastModified = fileInfo.LastWriteTimeUtc,
            ContentType = "application/octet-stream"
        };

        // 尝试加载保存的元数据
        var metadataPath = GetMetadataPath(filePath);
        if (File.Exists(metadataPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
                var savedMetadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (savedMetadata != null)
                {
                    if (savedMetadata.TryGetValue("ContentType", out var contentType))
                    {
                        metadata.ContentType = contentType;
                    }

                    metadata.Metadata = savedMetadata;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载元数据失败: {Key}", key);
            }
        }

        return metadata;
    }

    public Task<IEnumerable<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var searchPath = Path.Combine(_rootPath, prefix);
        var directory = Path.GetDirectoryName(searchPath) ?? _rootPath;

        if (!Directory.Exists(directory))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".metadata.json"))
            .Select(f => Path.GetRelativePath(_rootPath, f))
            .Where(f => f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(files);
    }

    public Task<string> GetPresignedUrlAsync(
        string key,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        // 对于本地文件系统，返回文件路径
        var filePath = GetFullPath(key);
        return Task.FromResult($"file://{filePath}");
    }

    public async Task<bool> CopyAsync(
        string sourceKey,
        string destinationKey,
        CancellationToken cancellationToken = default)
    {
        var sourcePath = GetFullPath(sourceKey);
        var destPath = GetFullPath(destinationKey);

        if (!File.Exists(sourcePath))
        {
            return false;
        }

        try
        {
            var destDirectory = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            File.Copy(sourcePath, destPath, overwrite: true);

            // 复制元数据文件
            var sourceMetadataPath = GetMetadataPath(sourcePath);
            if (File.Exists(sourceMetadataPath))
            {
                var destMetadataPath = GetMetadataPath(destPath);
                File.Copy(sourceMetadataPath, destMetadataPath, overwrite: true);
            }

            _logger.LogDebug("文件已复制: {SourceKey} -> {DestKey}", sourceKey, destinationKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制文件失败: {SourceKey} -> {DestKey}", sourceKey, destinationKey);
            return false;
        }
    }

    private string GetFullPath(string key)
    {
        // 规范化路径，移除前导斜杠
        key = key.TrimStart('/', '\\');

        // 防止路径遍历攻击
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, key));
        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"路径遍历攻击检测: {key}");
        }

        return fullPath;
    }

    private string GetMetadataPath(string filePath)
    {
        return filePath + ".metadata.json";
    }

    private async Task SaveMetadataAsync(
        string filePath,
        string contentType,
        IDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        var metadataPath = GetMetadataPath(filePath);
        var metadataDict = new Dictionary<string, string>(metadata)
        {
            ["ContentType"] = contentType
        };

        var json = System.Text.Json.JsonSerializer.Serialize(metadataDict, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
    }
}
