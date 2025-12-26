#if HAS_AWSSDK_S3
using Amazon.S3;
#endif
#if HAS_AZURE_STORAGE_BLOBS
using Azure.Storage.Blobs;
#endif
#if HAS_MINIO
using Minio;
#endif

namespace Old8Lang.PackageManager.Server.Storage;

/// <summary>
/// 存储提供程序工厂
/// </summary>
public class StorageProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StorageConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public StorageProviderFactory(
        IServiceProvider serviceProvider,
        StorageConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// 创建存储提供程序
    /// </summary>
    public IStorageProvider CreateProvider()
    {
        return _configuration.ProviderType switch
        {
            StorageProviderType.FileSystem => CreateFileSystemProvider(),
#if HAS_AWSSDK_S3
            StorageProviderType.S3 => CreateS3Provider(),
#endif
#if HAS_MINIO
            StorageProviderType.Minio => CreateMinioProvider(),
#endif
#if HAS_AZURE_STORAGE_BLOBS
            StorageProviderType.AzureBlob => CreateAzureBlobProvider(),
#endif
            _ => throw new NotSupportedException(
                $"不支持的存储提供程序类型: {_configuration.ProviderType}。" +
                $"请确保已安装相应的 NuGet 包并定义了相应的编译符号。")
        };
    }

    private IStorageProvider CreateFileSystemProvider()
    {
        var config = _configuration.FileSystem
            ?? throw new InvalidOperationException("FileSystem 配置未设置");

        var logger = _loggerFactory.CreateLogger<FileSystemStorageProvider>();
        return new FileSystemStorageProvider(config.RootPath, logger);
    }

#if HAS_AWSSDK_S3
    private IStorageProvider CreateS3Provider()
    {
        var config = _configuration.S3
            ?? throw new InvalidOperationException("S3 配置未设置");

        // 从服务容器获取 IAmazonS3 客户端
        var s3Client = _serviceProvider.GetService<IAmazonS3>();
        if (s3Client == null)
        {
            throw new InvalidOperationException("IAmazonS3 服务未注册");
        }

        var logger = _loggerFactory.CreateLogger<S3StorageProvider>();
        return new S3StorageProvider(s3Client, config.BucketName, logger);
    }
#endif

#if HAS_MINIO
    private IStorageProvider CreateMinioProvider()
    {
        var config = _configuration.Minio
            ?? throw new InvalidOperationException("Minio 配置未设置");

        // 从服务容器获取 IMinioClient
        var minioClient = _serviceProvider.GetService<IMinioClient>();
        if (minioClient == null)
        {
            throw new InvalidOperationException("IMinioClient 服务未注册");
        }

        var logger = _loggerFactory.CreateLogger<MinioStorageProvider>();
        return new MinioStorageProvider(minioClient, config.BucketName, logger);
    }
#endif

#if HAS_AZURE_STORAGE_BLOBS
    private IStorageProvider CreateAzureBlobProvider()
    {
        var config = _configuration.AzureBlob
            ?? throw new InvalidOperationException("AzureBlob 配置未设置");

        // 从服务容器获取 BlobContainerClient
        var containerClient = _serviceProvider.GetService<BlobContainerClient>();
        if (containerClient == null)
        {
            throw new InvalidOperationException("BlobContainerClient 服务未注册");
        }

        var logger = _loggerFactory.CreateLogger<AzureBlobStorageProvider>();
        return new AzureBlobStorageProvider(containerClient, logger);
    }
#endif
}

/// <summary>
/// 存储配置
/// </summary>
public class StorageConfiguration
{
    /// <summary>
    /// 存储提供程序类型
    /// </summary>
    public StorageProviderType ProviderType { get; set; } = StorageProviderType.FileSystem;

    /// <summary>
    /// 文件系统配置
    /// </summary>
    public FileSystemStorageConfiguration? FileSystem { get; set; }

    /// <summary>
    /// S3 配置
    /// </summary>
    public S3StorageConfiguration? S3 { get; set; }

    /// <summary>
    /// Minio 配置
    /// </summary>
    public MinioStorageConfiguration? Minio { get; set; }

    /// <summary>
    /// Azure Blob 配置
    /// </summary>
    public AzureBlobStorageConfiguration? AzureBlob { get; set; }
}

/// <summary>
/// 文件系统存储配置
/// </summary>
public class FileSystemStorageConfiguration
{
    /// <summary>
    /// 根目录路径
    /// </summary>
    public string RootPath { get; set; } = "packages";
}

/// <summary>
/// S3 存储配置
/// </summary>
public class S3StorageConfiguration
{
    /// <summary>
    /// AWS 区域
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Bucket 名称
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 访问密钥 ID
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// 秘密访问密钥
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// 服务 URL（可选，用于自定义端点）
    /// </summary>
    public string? ServiceUrl { get; set; }
}

/// <summary>
/// Minio 存储配置
/// </summary>
public class MinioStorageConfiguration
{
    /// <summary>
    /// 端点 URL
    /// </summary>
    public string Endpoint { get; set; } = "localhost:9000";

    /// <summary>
    /// Bucket 名称
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 访问密钥
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// 秘密密钥
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 是否使用 SSL
    /// </summary>
    public bool UseSSL { get; set; } = false;
}

/// <summary>
/// Azure Blob 存储配置
/// </summary>
public class AzureBlobStorageConfiguration
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 容器名称
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// 账户名称（使用 DefaultAzureCredential 时）
    /// </summary>
    public string? AccountName { get; set; }

    /// <summary>
    /// 使用托管标识
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;
}
