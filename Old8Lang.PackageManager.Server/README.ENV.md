# 环境变量配置快速入门

## 🚀 快速开始

### 1. 开发环境设置

```bash
# 进入服务器目录
cd Old8Lang.PackageManager.Server

# 复制环境变量示例文件
cp .env.example .env

# 编辑 .env 文件（使用您喜欢的编辑器）
# 大多数默认值已经适合开发环境，无需修改即可使用
```

### 2. 运行应用

```bash
dotnet run
```

应用启动时会自动加载 `.env` 文件中的配置。

## 📁 配置文件说明

| 文件 | 用途 | 是否提交到 Git |
|-----|------|----------------|
| `.env.example` | 开发环境配置示例 | ✅ 是 |
| `.env.docker` | Docker 部署配置示例 | ✅ 是 |
| `.env` | **实际使用的配置文件** | ❌ 否（包含敏感信息） |
| `appsettings.json` | 默认配置 | ✅ 是 |
| `CONFIGURATION.md` | 完整配置文档 | ✅ 是 |

## 🔧 常用配置示例

### 切换到 PostgreSQL 数据库

在 `.env` 文件中修改：

```bash
DatabaseProvider=PostgreSQL
ConnectionStrings__PostgresConnection=Host=localhost;Database=Old8LangPackageManager;Username=postgres;Password=YourPassword
```

### 启用 GitHub OAuth 登录

```bash
Authentication__OIDC__Enabled=true
Authentication__OIDC__Providers__GitHub__Enabled=true
Authentication__OIDC__Providers__GitHub__ClientId=your-github-client-id
Authentication__OIDC__Providers__GitHub__ClientSecret=your-github-client-secret
```

### 使用 MinIO 对象存储

```bash
Storage__ProviderType=Minio
Storage__Minio__Endpoint=localhost:9000
Storage__Minio__BucketName=old8lang-packages
Storage__Minio__AccessKey=minioadmin
Storage__Minio__SecretKey=minioadmin
```

## 🐳 Docker 部署

```bash
# 使用 Docker 配置模板
cp .env.docker .env

# 修改生产环境配置（重要！）
# 1. 修改数据库密码
# 2. 设置强随机 JWT 密钥
# 3. 配置 OAuth 客户端密钥（如需要）

# 启动服务
docker-compose up -d
```

## ⚠️ 安全提醒

### 生产环境必须修改的配置：

1. **JWT 密钥**
   ```bash
   Security__Jwt__SecretKey=<使用强随机字符串>
   ```

   生成密钥：
   ```bash
   openssl rand -base64 64
   ```

2. **数据库密码**
   ```bash
   ConnectionStrings__PostgresConnection=...Password=<使用强密码>
   ```

3. **OIDC 客户端密钥**（如启用）
   ```bash
   Authentication__OIDC__Providers__GitHub__ClientSecret=<真实密钥>
   ```

### 安全检查清单

- [ ] `.env` 文件已添加到 `.gitignore`
- [ ] 已修改默认的 JWT 密钥
- [ ] 数据库密码使用了强密码
- [ ] OIDC 密钥不是示例值
- [ ] 生产环境禁用了不必要的功能
- [ ] HTTPS 已启用
- [ ] CORS 策略已正确配置

## 📖 更多信息

查看完整的配置文档：[CONFIGURATION.md](./CONFIGURATION.md)

## 🔍 验证配置

启动应用后，检查以下端点：

- 健康检查：http://localhost:5000/health
- API 文档：http://localhost:5000/swagger
- 查看启动日志，确认 .env 文件已加载

## 💡 提示

- 环境变量优先级最高，会覆盖 `appsettings.json` 中的配置
- 使用双下划线 `__` 表示配置层级
- `.env` 文件只在开发环境自动加载，生产环境建议使用真实的环境变量或 Docker secrets
