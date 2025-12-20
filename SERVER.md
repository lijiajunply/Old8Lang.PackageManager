# Old8Lang Package Manager Server 服务端

## 📖 概述

Old8Lang Package Manager Server 是一个完整的包管理器服务端实现，提供包存储、分发、管理和安全验证等功能。该服务端基于 ASP.NET Core 10.0 构建，支持现代化的 RESTful API 和容器化部署。

## 🏗️ 架构设计

### 核心组件

1. **API 控制器** - 处理 HTTP 请求
2. **服务层** - 业务逻辑处理
3. **数据访问层** - 数据库操作
4. **中间件** - 认证和安全
5. **存储层** - 包文件管理

### 技术栈

- **框架**: ASP.NET Core 10.0
- **数据库**: SQLite (可扩展到 SQL Server/PostgreSQL)
- **ORM**: Entity Framework Core 8.0
- **API 文档**: Swagger/OpenAPI
- **容器化**: Docker
- **认证**: API 密钥认证
- **安全**: 包签名和完整性验证

## 🚀 快速开始

### 环境要求

- .NET 10.0 SDK
- SQLite (开发环境)
- Git

### 本地开发

```bash
# 克隆仓库
git clone <repository-url>
cd Old8Lang.PackageManager

# 运行部署脚本
./deploy.sh

# 或者手动操作
dotnet restore
dotnet build
dotnet ef database update
dotnet run --project Old8Lang.PackageManager.Server
```

### Docker 部署

```bash
# 构建镜像
docker build -t old8lang-package-manager .

# 运行容器
docker run -p 5000:80 -p 5001:443 old8lang-package-manager

# 或使用 Docker Compose
docker-compose up -d
```

## 📡 API 接口

### 服务索引
```http
GET /v3/index.json
```
返回可用的服务资源列表。

### 包搜索
```http
GET /v3/search?q=logger&skip=0&take=20&prerelease=false
```
根据关键词搜索包。

### 获取包信息
```http
GET /v3/package/{id}?version=1.0.0
GET /v3/package/{id}
```
获取包的详细信息。

### 上传包
```http
POST /v3/package
Content-Type: multipart/form-data
Authorization: Bearer <api_key>
```
上传新的包版本。

### 下载包
```http
GET /v3/package/{id}/{version}/download
```
下载指定版本的包。

### 删除包
```http
DELETE /v3/package/{id}/{version}
Authorization: Bearer <api_key>
```
删除指定版本的包。

### API 密钥管理
```http
GET    /api/v1/apikeys          # 获取所有 API 密钥
POST   /api/v1/apikeys          # 创建新 API 密钥
DELETE /api/v1/apikeys/{id}     # 撤销 API 密钥
POST   /api/v1/apikeys/validate # 验证 API 密钥
```

### 统计信息
```http
GET /api/v1/statistics           # 服务统计
GET /api/v1/statistics/downloads/trend?days=30 # 下载趋势
```

## 🔐 安全机制

### API 密钥认证

服务端支持多种 API 密钥传递方式：

1. **Authorization Header**
   ```http
   Authorization: Bearer <api_key>
   ```

2. **查询参数**
   ```http
   GET /v3/search?api_key=<api_key>
   ```

3. **自定义 Header**
   ```http
   X-API-Key: <api_key>
   ```

### 权限范围

- `package:read` - 读取包信息（默认权限）
- `package:write` - 上传和删除包
- `admin:all` - 管理所有功能

### 包签名验证

- 支持 SHA256/SHA512 哈希算法
- 可配置的信任证书列表
- 包完整性自动验证

## ⚙️ 配置选项

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=packages.db"
  },
  "PackageStorage": {
    "StoragePath": "packages",
    "MaxPackageSize": 104857600,
    "AllowedExtensions": [ ".o8pkg" ],
    "EnableCompression": true
  },
  "Api": {
    "Version": "3.0.0",
    "ServiceName": "Old8Lang Package Manager",
    "BaseUrl": "https://localhost:5001",
    "RequireApiKey": false,
    "RateLimitPerMinute": 100
  },
  "Security": {
    "EnablePackageSigning": false,
    "TrustedCertificates": [],
    "EnableChecksumValidation": true,
    "AllowedHashAlgorithms": [ "SHA256", "SHA512" ]
  }
}
```

### 环境变量

| 变量名 | 描述 | 默认值 |
|---------|------|--------|
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | `Development` |
| `ASPNETCORE_URLS` | 监听地址 | `http://+:80;https://+:443` |
| `DB_CONNECTION_STRING` | 数据库连接 | `Data Source=packages.db` |

## 📦 包管理

### 上传包流程

1. **验证 API 密钥** - 检查权限
2. **验证文件格式** - 检查文件扩展名和大小
3. **提取包信息** - 解析 package.json
4. **存储包文件** - 保存到存储目录
5. **计算校验和** - 生成 SHA256 哈希
6. **保存元数据** - 存储到数据库
7. **返回结果** - 包详细信息

### 下载包流程

1. **验证包存在** - 检查数据库记录
2. **获取文件流** - 从存储目录读取
3. **更新下载计数** - 增加统计信息
4. **返回文件流** - 流式传输给客户端

### 包验证

- **格式验证** - ZIP 文件结构检查
- **完整性验证** - SHA256/SHA512 校验
- **签名验证** - 数字证书验证（可选）
- **依赖验证** - 依赖关系检查

## 🗄️ 数据模型

### 包实体 (PackageEntity)

```csharp
public class PackageEntity
{
    public int Id { get; set; }
    public string PackageId { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public string License { get; set; }
    public string ProjectUrl { get; set; }
    public string Checksum { get; set; }
    public long Size { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int DownloadCount { get; set; }
    public bool IsListed { get; set; }
    public bool IsPrerelease { get; set; }
    
    // 导航属性
    public ICollection<PackageTagEntity> PackageTags { get; set; }
    public ICollection<PackageDependencyEntity> PackageDependencies { get; set; }
    public ICollection<PackageFileEntity> Files { get; set; }
}
```

## 🚀 部署指南

### 生产环境部署

1. **环境准备**
   ```bash
   # 安装 .NET 10.0 Runtime
   # 配置防火墙规则
   # 准备 SSL 证书
   ```

2. **应用部署**
   ```bash
   # 构建发布版本
   dotnet publish -c Release -o /var/www/o8pm
   
   # 配置系统服务
   sudo systemctl enable o8pm
   sudo systemctl start o8pm
   ```

3. **反向代理** (Nginx)
   ```nginx
   server {
       listen 80;
       server_name packages.old8lang.org;
       return 301 https://$server_name$request_uri;
   }
   
   server {
       listen 443 ssl;
       server_name packages.old8lang.org;
       
       ssl_certificate /path/to/cert.pem;
       ssl_certificate_key /path/to/key.pem;
       
       location / {
           proxy_pass http://localhost:5000;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }
   }
   ```

### 监控和日志

- **健康检查**: `/health` 端点
- **日志记录**: 结构化日志输出
- **性能监控**: 下载计数和响应时间
- **错误追踪**: 异常自动记录

## 🔧 扩展功能

### 计划中的功能

- [ ] 包版本管理和更新通知
- [ ] 依赖关系可视化
- [ ] 包质量评分系统
- [ ] 私有包源支持
- [ ] CDN 集成
- [ ] 包分析工具
- [ ] 用户系统集成

### API 扩展

服务端设计为模块化架构，易于扩展：

```csharp
// 添加新的服务
builder.Services.AddScoped<IPackageAnalyticsService, PackageAnalyticsService>();

// 添加新的控制器
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AnalyticsController).Assembly);
```

## 📊 性能优化

### 缓存策略

- **元数据缓存** - 包信息内存缓存
- **搜索缓存** - 搜索结果缓存
- **文件缓存** - 静态文件 CDN 缓存

### 数据库优化

- **索引优化** - 包 ID、版本、发布时间
- **查询优化** - 使用 EF Core 查询优化
- **连接池** - 数据库连接池管理

## 🤝 贡献指南

欢迎贡献代码！请遵循以下步骤：

1. Fork 项目
2. 创建功能分支
3. 提交更改
4. 创建 Pull Request

### 代码规范

- 遵循 C# 编码规范
- 添加单元测试
- 更新文档
- 通过 CI 检查

## 📄 许可证

本项目采用 MIT 许可证，详见 [LICENSE](LICENSE) 文件。

## 🆘 支持

- **文档**: [API 文档](http://localhost:5001/swagger)
- **问题反馈**: GitHub Issues
- **社区讨论**: [Discussions](https://github.com/old8lang/o8pm/discussions)

---

这个服务端为 Old8Lang 生态系统提供了完整、安全、高性能的包管理解决方案，支持从小型团队到企业级部署的各种需求。