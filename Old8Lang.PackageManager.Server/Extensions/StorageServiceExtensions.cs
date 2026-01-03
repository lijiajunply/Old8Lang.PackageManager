#if HAS_AWSSDK_S3
using Amazon.S3;
using Amazon;
#endif
#if HAS_AZURE_STORAGE_BLOBS
using Azure.Storage.Blobs;
using Azure.Identity;
#endif
#if HAS_MINIO
using Minio;
#endif
using Old8Lang.PackageManager.Server.Storage;

namespace Old8Lang.PackageManager.Server.Extensions;

/// <summary>
/// 存储服务注册扩展
/// </summary>
public static class StorageServiceExtensions
{
    /// <summary>
    /// 添加存储服务
    /// </summary>
    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 配置存储选项
        services.Configure<StorageConfiguration>(configuration.GetSection("Storage"));
        var storageConfig = configuration.GetSection("Storage").Get<StorageConfiguration>()
            ?? new StorageConfiguration();

        // 注册 StorageConfiguration 为单例，供 StorageProviderFactory 使用
        services.AddSingleton(storageConfig);

        // 根据配置的提供程序类型注册相应的客户端
        switch (storageConfig.ProviderType)
        {
#if HAS_AWSSDK_S3
            case StorageProviderType.S3:
                RegisterS3Services(services, storageConfig.S3!);
                break;
#endif

#if HAS_MINIO
            case StorageProviderType.Minio:
                RegisterMinioServices(services, storageConfig.Minio!);
                break;
#endif

#if HAS_AZURE_STORAGE_BLOBS
            case StorageProviderType.AzureBlob:
                RegisterAzureBlobServices(services, storageConfig.AzureBlob!);
                break;
#endif

            case StorageProviderType.FileSystem:
                // 文件系统不需要额外的客户端注册
                break;

            default:
                throw new NotSupportedException(
                    $"存储提供程序 '{storageConfig.ProviderType}' 未编译。" +
                    $"请安装相应的 NuGet 包并在项目中定义相应的编译符号。");
        }

        // 注册存储提供程序工厂
        services.AddSingleton<StorageProviderFactory>();

        // 注册 IStorageProvider
        services.AddSingleton<IStorageProvider>(sp =>
        {
            var factory = sp.GetRequiredService<StorageProviderFactory>();
            return factory.CreateProvider();
        });

        return services;
    }

#if HAS_AWSSDK_S3
    private static void RegisterS3Services(IServiceCollection services, S3StorageConfiguration config)
    {
        // 配置 AWS S3 客户端
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var clientConfig = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(config.Region)
            };

            // 如果配置了自定义服务 URL，使用它（适用于 S3 兼容服务）
            if (!string.IsNullOrEmpty(config.ServiceUrl))
            {
                clientConfig.ServiceURL = config.ServiceUrl;
                clientConfig.ForcePathStyle = true;
            }

            // 使用凭证创建客户端
            if (!string.IsNullOrEmpty(config.AccessKeyId) && !string.IsNullOrEmpty(config.SecretAccessKey))
            {
                return new AmazonS3Client(config.AccessKeyId, config.SecretAccessKey, clientConfig);
            }

            // 否则使用默认凭证链（环境变量、IAM 角色等）
            return new AmazonS3Client(clientConfig);
        });
    }
#endif

#if HAS_MINIO
    private static void RegisterMinioServices(IServiceCollection services, MinioStorageConfiguration config)
    {
        // 配置 Minio 客户端
        services.AddSingleton<IMinioClient>(sp =>
        {
            var client = new MinioClient()
                .WithEndpoint(config.Endpoint)
                .WithCredentials(config.AccessKey, config.SecretKey);

            if (config.UseSSL)
            {
                client = client.WithSSL();
            }

            return client.Build();
        });
    }
#endif

#if HAS_AZURE_STORAGE_BLOBS
    private static void RegisterAzureBlobServices(IServiceCollection services, AzureBlobStorageConfiguration config)
    {
        // 配置 Azure Blob 容器客户端
        services.AddSingleton<BlobContainerClient>(sp =>
        {
            if (config.UseManagedIdentity && !string.IsNullOrEmpty(config.AccountName))
            {
                // 使用托管标识
                var blobServiceClient = new BlobServiceClient(
                    new Uri($"https://{config.AccountName}.blob.core.windows.net"),
                    new DefaultAzureCredential());

                return blobServiceClient.GetBlobContainerClient(config.ContainerName);
            }
            else if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                // 使用连接字符串
                return new BlobContainerClient(config.ConnectionString, config.ContainerName);
            }

            throw new InvalidOperationException("Azure Blob 配置无效：需要 ConnectionString 或 (AccountName + UseManagedIdentity)");
        });
    }
#endif
}
