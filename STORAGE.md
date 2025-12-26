# 存储抽象层

Old8Lang Package Manager 实现了一个灵活的存储抽象层，支持多种存储后端。这允许您根据需求选择最适合的存储方案，从本地文件系统到云存储服务。

## 支持的存储后端

### 1. 本地文件系统 (FileSystem)
- **适用场景**: 开发环境、测试环境、小规模部署
- **优点**: 简单、无需额外配置、快速
- **缺点**: 不支持水平扩展、需要备份管理

### 2. AWS S3
- **适用场景**: AWS 云环境、生产部署
- **优点**: 高可用性、无限扩展、CDN 集成
- **缺点**: 需要 AWS 账户、按使用量计费

### 3. Minio
- **适用场景**: 私有云、自托管、S3 兼容需求
- **优点**: 开源、S3 API 兼容、可自托管
- **缺点**: 需要维护 Minio 服务器

### 4. Azure Blob Storage
- **适用场景**: Azure 云环境
- **优点**: 与 Azure 服务集成、托管标识支持
- **缺点**: 需要 Azure 订阅

## 配置

### 配置文件结构

在 `appsettings.json` 中配置存储后端：

```json
{
  "Storage": {
    "ProviderType": "FileSystem",
    "FileSystem": {
      "RootPath": "packages"
    },
    "S3": {
      "Region": "us-east-1",
      "BucketName": "old8lang-packages",
      "AccessKeyId": "YOUR_ACCESS_KEY",
      "SecretAccessKey": "YOUR_SECRET_KEY",
      "ServiceUrl": null
    },
    "Minio": {
      "Endpoint": "localhost:9000",
      "BucketName": "old8lang-packages",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "UseSSL": false
    },
    "AzureBlob": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
      "ContainerName": "old8lang-packages",
      "AccountName": null,
      "UseManagedIdentity": false
    }
  }
}
```

### 环境变量配置

您也可以使用环境变量覆盖配置：

```bash
# 选择存储提供程序
export Storage__ProviderType=S3

# S3 配置
export Storage__S3__Region=us-east-1
export Storage__S3__BucketName=old8lang-packages
export Storage__S3__AccessKeyId=YOUR_ACCESS_KEY
export Storage__S3__SecretAccessKey=YOUR_SECRET_KEY

# Minio 配置
export Storage__Minio__Endpoint=minio.example.com:9000
export Storage__Minio__BucketName=old8lang-packages
export Storage__Minio__AccessKey=YOUR_ACCESS_KEY
export Storage__Minio__SecretKey=YOUR_SECRET_KEY
export Storage__Minio__UseSSL=true

# Azure Blob 配置
export Storage__AzureBlob__ConnectionString="DefaultEndpointsProtocol=https;AccountName=..."
export Storage__AzureBlob__ContainerName=old8lang-packages
```

## 使用示例

### 切换到本地文件系统

```json
{
  "Storage": {
    "ProviderType": "FileSystem",
    "FileSystem": {
      "RootPath": "/var/lib/old8lang/packages"
    }
  }
}
```

### 切换到 AWS S3

```json
{
  "Storage": {
    "ProviderType": "S3",
    "S3": {
      "Region": "us-west-2",
      "BucketName": "my-packages-bucket",
      "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
      "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
    }
  }
}
```

**使用 IAM 角色（推荐）**:

如果在 EC2 实例上运行，可以使用 IAM 角色，无需配置密钥：

```json
{
  "Storage": {
    "ProviderType": "S3",
    "S3": {
      "Region": "us-west-2",
      "BucketName": "my-packages-bucket",
      "AccessKeyId": "",
      "SecretAccessKey": ""
    }
  }
}
```

### 切换到 Minio

```json
{
  "Storage": {
    "ProviderType": "Minio",
    "Minio": {
      "Endpoint": "minio.example.com:9000",
      "BucketName": "packages",
      "AccessKey": "YOUR_MINIO_ACCESS_KEY",
      "SecretKey": "YOUR_MINIO_SECRET_KEY",
      "UseSSL": true
    }
  }
}
```

**Docker Compose 示例**:

```yaml
version: '3.8'
services:
  minio:
    image: minio/minio:latest
    ports:
      - "9000:9000"
      - "9001:9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    command: server /data --console-address ":9001"
    volumes:
      - minio-data:/data

  package-manager:
    build: .
    ports:
      - "5000:5000"
    environment:
      Storage__ProviderType: Minio
      Storage__Minio__Endpoint: minio:9000
      Storage__Minio__BucketName: old8lang-packages
      Storage__Minio__AccessKey: minioadmin
      Storage__Minio__SecretKey: minioadmin
      Storage__Minio__UseSSL: false
    depends_on:
      - minio

volumes:
  minio-data:
```

### 切换到 Azure Blob Storage

**使用连接字符串**:

```json
{
  "Storage": {
    "ProviderType": "AzureBlob",
    "AzureBlob": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=mystorageaccount;AccountKey=...;EndpointSuffix=core.windows.net",
      "ContainerName": "packages"
    }
  }
}
```

**使用托管标识（推荐用于 Azure VM/App Service）**:

```json
{
  "Storage": {
    "ProviderType": "AzureBlob",
    "AzureBlob": {
      "AccountName": "mystorageaccount",
      "ContainerName": "packages",
      "UseManagedIdentity": true
    }
  }
}
```

## NuGet 包依赖

**重要**: 云存储提供程序是可选的。默认情况下,只有本地文件系统存储可用。

### 快速开始

如果您只需要本地文件系统存储,**无需安装任何额外的 NuGet 包**,系统开箱即用。

### 安装可选云存储依赖

根据您需要的存储后端,安装相应的 NuGet 包:

```bash
# AWS S3
dotnet add package AWSSDK.S3

# Minio
dotnet add package Minio

# Azure Blob Storage
dotnet add package Azure.Storage.Blobs
dotnet add package Azure.Identity
```

### 启用云存储提供程序

⚠️ **关键步骤**: 安装 NuGet 包后,必须在项目文件中定义相应的编译符号才能启用云存储支持。

编辑 `Old8Lang.PackageManager.Server/Old8Lang.PackageManager.Server.csproj`,在 `<PropertyGroup>` 中添加:

```xml
<PropertyGroup>
  <!-- 启用 AWS S3 支持 -->
  <DefineConstants>$(DefineConstants);HAS_AWSSDK_S3</DefineConstants>

  <!-- 启用 Minio 支持 -->
  <DefineConstants>$(DefineConstants);HAS_MINIO</DefineConstants>

  <!-- 启用 Azure Blob 支持 -->
  <DefineConstants>$(DefineConstants);HAS_AZURE_STORAGE_BLOBS</DefineConstants>
</PropertyGroup>
```

或者一次性启用所有提供程序:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);HAS_AWSSDK_S3;HAS_MINIO;HAS_AZURE_STORAGE_BLOBS</DefineConstants>
</PropertyGroup>
```

### 验证配置

重新编译项目以验证配置:

```bash
dotnet build Old8Lang.PackageManager.sln
```

如果配置正确但缺少 NuGet 包,您会看到编译错误。如果配置的存储提供程序类型未启用编译符号,运行时会抛出 `NotSupportedException` 异常。

## 高级用法

### 在代码中直接使用存储提供程序

```csharp
using Old8Lang.PackageManager.Server.Storage;

public class MyService
{
    private readonly IStorageProvider _storage;

    public MyService(IStorageProvider storage)
    {
        _storage = storage;
    }

    public async Task UploadFileAsync(string key, Stream stream)
    {
        var metadata = new Dictionary<string, string>
        {
            ["UploadedBy"] = "MyService",
            ["Timestamp"] = DateTime.UtcNow.ToString("O")
        };

        await _storage.UploadAsync(key, stream, "application/octet-stream", metadata);
    }

    public async Task<Stream?> DownloadFileAsync(string key)
    {
        return await _storage.DownloadAsync(key);
    }

    public async Task<string> GetDownloadUrlAsync(string key)
    {
        // 生成 1 小时有效的预签名 URL
        return await _storage.GetPresignedUrlAsync(key, TimeSpan.FromHours(1));
    }
}
```

### 切换到新的存储服务

要使用新的存储抽象层，在 Program.cs 中更新服务注册：

```csharp
// 使用新的存储抽象层
builder.Services.AddScoped<IPackageStorageService, Storage.AbstractPackageStorageService>();

// 而不是原来的
// builder.Services.AddScoped<IPackageStorageService, PackageStorageService>();
```

## 性能优化

### S3/Minio

- **启用多部分上传**: 对于大文件，使用多部分上传提高上传速度
- **使用 CloudFront CDN**: 配置 CDN 加速全球下载
- **生命周期策略**: 自动归档或删除旧版本包

### Azure Blob

- **启用 CDN**: 使用 Azure CDN 加速内容分发
- **访问层**: 根据访问频率选择热/冷存储层
- **缓存控制**: 设置适当的缓存头

### 本地文件系统

- **SSD 存储**: 使用 SSD 提高 I/O 性能
- **定期备份**: 实施自动备份策略
- **磁盘配额**: 监控磁盘使用情况

## 迁移指南

### 从本地文件系统迁移到 S3

1. 配置 S3 存储后端
2. 使用 AWS CLI 同步文件：

```bash
aws s3 sync ./packages s3://old8lang-packages/
```

3. 更新配置切换到 S3
4. 重启应用

### 从 S3 迁移到 Minio

1. 设置 Minio 服务器
2. 使用 MinIO Client (mc) 镜像数据：

```bash
mc alias set s3 https://s3.amazonaws.com ACCESS_KEY SECRET_KEY
mc alias set myminio http://minio:9000 minioadmin minioadmin
mc mirror s3/old8lang-packages myminio/old8lang-packages
```

3. 更新配置切换到 Minio
4. 重启应用

## 安全建议

1. **使用 IAM 角色/托管标识**: 避免在配置文件中硬编码密钥
2. **启用 HTTPS**: 对所有云存储启用 SSL/TLS
3. **访问控制**: 使用最小权限原则配置 IAM 策略
4. **加密**: 启用服务端加密（S3-SSE、Azure SSE）
5. **密钥轮换**: 定期轮换访问密钥
6. **日志审计**: 启用访问日志记录

## 故障排除

### 无法连接到 S3

```
错误: Unable to connect to S3
解决: 检查 Region、AccessKeyId、SecretAccessKey 是否正确
      检查网络连接和防火墙规则
```

### Minio bucket 不存在

```
错误: Bucket does not exist
解决: MinioStorageProvider 会自动创建 bucket
      检查 Minio 服务器是否运行
      验证访问凭证是否正确
```

### Azure Blob 认证失败

```
错误: Authentication failed
解决: 检查 ConnectionString 是否正确
      如果使用托管标识，确保已分配正确的 RBAC 角色
```

## 监控和指标

建议监控以下指标：

- 上传/下载成功率
- 平均响应时间
- 存储使用量
- 请求数和带宽
- 错误率

## 参考资料

- [AWS S3 文档](https://docs.aws.amazon.com/s3/)
- [Minio 文档](https://min.io/docs/minio/linux/index.html)
- [Azure Blob Storage 文档](https://docs.microsoft.com/en-us/azure/storage/blobs/)
