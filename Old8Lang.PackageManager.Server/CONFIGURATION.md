# 环境变量配置文档

本文档详细说明了 Old8Lang Package Manager Server 的环境变量配置方式。

## 配置优先级

配置加载的优先级从高到低：

1. **环境变量** - 最高优先级，会覆盖所有其他配置
2. **.env 文件** - 开发环境使用，不应提交到版本控制
3. **appsettings.{Environment}.json** - 环境特定配置
4. **appsettings.json** - 默认配置

## 快速开始

### 开发环境

1. 复制示例文件：
   ```bash
   cd Old8Lang.PackageManager.Server
   cp .env.example .env
   ```

2. 修改 `.env` 文件中的配置值

3. 启动应用：
   ```bash
   dotnet run
   ```

### Docker 部署

1. 复制 Docker 配置：
   ```bash
   cp .env.docker .env
   ```

2. 修改必要的配置值（数据库密码、JWT 密钥等）

3. 使用 Docker Compose 启动：
   ```bash
   docker-compose up -d
   ```

## 环境变量命名规则

ASP.NET Core 使用双下划线 `__` 来表示配置层级。例如：

- JSON 配置：`"ConnectionStrings": { "DefaultConnection": "..." }`
- 环境变量：`ConnectionStrings__DefaultConnection=...`

## 核心配置项

### 数据库配置

| 环境变量 | 说明 | 默认值 | 示例 |
|---------|------|-------|------|
| `DatabaseProvider` | 数据库提供者 | `SQLite` | `SQLite`, `PostgreSQL`, `SQLServer` |
| `ConnectionStrings__DefaultConnection` | 默认连接字符串 | `Data Source=packages.db` | `Host=localhost;Database=...` |
| `ConnectionStrings__PostgresConnection` | PostgreSQL 连接字符串 | - | `Host=postgres;Port=5432;Database=...` |
| `ConnectionStrings__Redis` | Redis 连接字符串 | `localhost:6379` | `redis:6379` |

### 服务器配置

| 环境变量 | 说明 | 默认值 |
|---------|------|-------|
| `Kestrel__Endpoints__Http__Url` | HTTP 端点 | `http://localhost:5000` |
| `Kestrel__Endpoints__Https__Url` | HTTPS 端点 | `https://localhost:5001` |
| `AllowedHosts` | 允许的主机 | `*` |
| `ASPNETCORE_ENVIRONMENT` | 应用环境 | `Development` |

### 存储配置

| 环境变量 | 说明 | 默认值 |
|---------|------|-------|
| `PackageStorage__StoragePath` | 包存储路径 | `packages` |
| `PackageStorage__MaxPackageSize` | 最大包大小(字节) | `104857600` (100MB) |
| `PackageStorage__EnableCompression` | 启用压缩 | `true` |
| `Storage__ProviderType` | 存储提供者类型 | `FileSystem` |

#### 存储提供者配置

**FileSystem**
```bash
Storage__ProviderType=FileSystem
Storage__FileSystem__RootPath=packages
```

**AWS S3**
```bash
Storage__ProviderType=S3
Storage__S3__Region=us-east-1
Storage__S3__BucketName=old8lang-packages
Storage__S3__AccessKeyId=YOUR_ACCESS_KEY
Storage__S3__SecretAccessKey=YOUR_SECRET_KEY
```

**MinIO**
```bash
Storage__ProviderType=Minio
Storage__Minio__Endpoint=localhost:9000
Storage__Minio__BucketName=old8lang-packages
Storage__Minio__AccessKey=minioadmin
Storage__Minio__SecretKey=minioadmin
Storage__Minio__UseSSL=false
```

**Azure Blob**
```bash
Storage__ProviderType=AzureBlob
Storage__AzureBlob__ConnectionString=DefaultEndpointsProtocol=https;...
Storage__AzureBlob__ContainerName=old8lang-packages
```

### API 配置

| 环境变量 | 说明 | 默认值 |
|---------|------|-------|
| `Api__Version` | API 版本 | `3.0.0` |
| `Api__ServiceName` | 服务名称 | `Old8Lang Package Manager` |
| `Api__BaseUrl` | 基础 URL | `https://localhost:5001` |
| `Api__RequireApiKey` | 需要 API 密钥 | `false` |
| `Api__RequireAuthentication` | 需要认证 | `true` |
| `Api__RateLimitPerMinute` | 每分钟速率限制 | `100` |

### 安全配置

| 环境变量 | 说明 | 默认值 |
|---------|------|-------|
| `Security__EnablePackageSigning` | 启用包签名 | `false` |
| `Security__RequireTrustedCertificates` | 需要受信证书 | `false` |
| `Security__ValidateCertificateChain` | 验证证书链 | `false` |
| `Security__EnableChecksumValidation` | 启用校验和验证 | `true` |

#### JWT 配置（重要：生产环境必须修改）

| 环境变量 | 说明 | 默认值 |
|---------|------|-------|
| `Security__Jwt__Issuer` | JWT 签发者 | `Old8LangPackageManager` |
| `Security__Jwt__Audience` | JWT 受众 | `Old8LangPackageManager` |
| `Security__Jwt__SecretKey` | **JWT 密钥（必须修改）** | `CHANGE_THIS_SECRET_KEY_FOR_PRODUCTION` |
| `Security__Jwt__ExpirationMinutes` | Token 过期时间(分钟) | `1440` (24小时) |
| `Security__Jwt__RefreshExpirationDays` | 刷新 Token 过期时间(天) | `30` |

⚠️ **安全警告**：生产环境必须设置强随机字符串作为 JWT 密钥！

生成安全密钥示例：
```bash
# 使用 openssl 生成随机密钥
openssl rand -base64 64

# 或使用 PowerShell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

### 认证配置

| 环境变量 | 说明 | 默认值 |
|---------|------|-------|
| `Authentication__EnableRegistration` | 启用注册 | `true` |
| `Authentication__RequireEmailVerification` | 需要邮箱验证 | `false` |
| `Authentication__AllowGuestUpload` | 允许访客上传 | `false` |
| `Authentication__MaxPackagesPerUser` | 每用户最大包数 | `100` |
| `Authentication__MaxPackageSizePerUser` | 每用户最大包大小 | `1073741824` (1GB) |

### OIDC OAuth 配置

**启用 OIDC**
```bash
Authentication__OIDC__Enabled=true
```

**GitHub OAuth**
```bash
Authentication__OIDC__Providers__GitHub__Enabled=true
Authentication__OIDC__Providers__GitHub__ClientId=your-github-client-id
Authentication__OIDC__Providers__GitHub__ClientSecret=your-github-client-secret
```

**Google OAuth**
```bash
Authentication__OIDC__Providers__Google__Enabled=true
Authentication__OIDC__Providers__Google__ClientId=your-google-client-id
Authentication__OIDC__Providers__Google__ClientSecret=your-google-client-secret
```

**自定义 OIDC 提供者**
```bash
Authentication__OIDC__Providers__Custom__Enabled=true
Authentication__OIDC__Providers__Custom__ClientId=your-client-id
Authentication__OIDC__Providers__Custom__ClientSecret=your-client-secret
Authentication__OIDC__Providers__Custom__Authority=https://your-oidc-provider.com
```

### Redis 配置

| 环境变量 | 说明 | 默认值 |
|---------|------|-------|
| `Redis__Enabled` | 启用 Redis | `true` |
| `Redis__ConnectionString` | Redis 连接字符串 | `localhost:6379` |
| `Redis__Database` | 数据库编号 | `0` |
| `Redis__KeyPrefix` | 键前缀 | `o8pm:` |
| `Redis__DefaultExpiration` | 默认过期时间 | `01:00:00` |

### 日志配置

| 环境变量 | 说明 | 默认值 |
|---------|------|-------|
| `Logging__LogLevel__Default` | 默认日志级别 | `Information` |
| `Logging__LogLevel__Microsoft.AspNetCore` | ASP.NET Core 日志级别 | `Warning` |
| `Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command` | EF Core SQL 日志级别 | `Information` |

日志级别选项：`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`

## 部署场景示例

### 场景 1：本地开发 (SQLite)

创建 `.env` 文件：
```bash
DatabaseProvider=SQLite
ConnectionStrings__DefaultConnection=Data Source=packages.db
Kestrel__Endpoints__Http__Url=http://localhost:5000
ASPNETCORE_ENVIRONMENT=Development
```

### 场景 2：Docker + PostgreSQL

创建 `.env` 文件：
```bash
DatabaseProvider=PostgreSQL
ConnectionStrings__PostgresConnection=Host=postgres;Port=5432;Database=Old8LangPackageManager;Username=postgres;Password=YourSecurePassword
ConnectionStrings__Redis=redis:6379
Kestrel__Endpoints__Http__Url=http://0.0.0.0:5000
PackageStorage__StoragePath=/app/data/packages
Security__Jwt__SecretKey=YOUR_GENERATED_SECRET_KEY_HERE
ASPNETCORE_ENVIRONMENT=Production
```

### 场景 3：生产环境 + S3 存储

```bash
DatabaseProvider=PostgreSQL
ConnectionStrings__PostgresConnection=Host=prod-db.example.com;...
Storage__ProviderType=S3
Storage__S3__Region=us-east-1
Storage__S3__BucketName=prod-packages
Storage__S3__AccessKeyId=YOUR_ACCESS_KEY
Storage__S3__SecretAccessKey=YOUR_SECRET_KEY
Security__Jwt__SecretKey=YOUR_GENERATED_SECRET_KEY_HERE
Security__EnablePackageSigning=true
Security__RequireTrustedCertificates=true
Authentication__OIDC__Enabled=true
Authentication__OIDC__Providers__GitHub__Enabled=true
Authentication__OIDC__Providers__GitHub__ClientId=production-github-id
Authentication__OIDC__Providers__GitHub__ClientSecret=production-github-secret
ASPNETCORE_ENVIRONMENT=Production
```

## 使用 Docker Secrets (推荐用于生产环境)

对于 Docker Swarm 或 Kubernetes 部署，可以使用 secrets 管理敏感信息：

### Docker Compose with Secrets

```yaml
services:
  backend:
    image: old8lang/package-manager:latest
    environment:
      - ConnectionStrings__PostgresConnection_FILE=/run/secrets/db_connection
      - Security__Jwt__SecretKey_FILE=/run/secrets/jwt_secret
    secrets:
      - db_connection
      - jwt_secret

secrets:
  db_connection:
    external: true
  jwt_secret:
    external: true
```

## 验证配置

启动应用后，可以通过以下方式验证配置：

1. 检查启动日志，确认是否加载了 `.env` 文件
2. 访问健康检查端点：`GET http://localhost:5000/health`
3. 查看 Swagger 文档：`http://localhost:5000/swagger`

## 故障排查

### 配置未生效

1. 确认环境变量名称使用双下划线 `__`
2. 检查 `.env` 文件是否在正确的目录
3. 确认没有拼写错误
4. 重启应用以加载新配置

### 数据库连接失败

1. 检查 `DatabaseProvider` 是否与连接字符串匹配
2. 验证数据库服务是否正在运行
3. 检查防火墙规则
4. 确认连接字符串格式正确

### JWT 认证失败

1. 确认 `Security__Jwt__SecretKey` 已设置且足够长（至少 32 字符）
2. 检查 Issuer 和 Audience 配置是否一致
3. 验证 Token 是否过期

## 安全最佳实践

1. ✅ **永远不要**提交 `.env` 文件到版本控制
2. ✅ 在 `.gitignore` 中添加 `.env`
3. ✅ 生产环境使用强随机 JWT 密钥
4. ✅ 使用环境变量或 secrets 管理敏感信息
5. ✅ 定期轮换密钥和凭证
6. ✅ 限制数据库用户权限
7. ✅ 启用 HTTPS 在生产环境
8. ✅ 配置适当的 CORS 策略

## 相关文件

- `.env.example` - 开发环境示例配置
- `.env.docker` - Docker 部署示例配置
- `appsettings.json` - 默认配置文件
- `appsettings.Development.json` - 开发环境配置
- `appsettings.Production.json` - 生产环境配置（如需要可创建）

## 参考资源

- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Environment Variables in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#environment-variables)
- [Docker Secrets](https://docs.docker.com/engine/swarm/secrets/)
