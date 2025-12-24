# OIDC 用户认证和 PostgreSQL 集成

## 🚀 新功能概述

我们已经成功集成了以下新功能：

1. **🔐 OIDC 用户认证系统** - 支持 GitHub、Google 和自定义 OIDC 提供商
2. **🐘 PostgreSQL 数据库支持** - 可配置使用 PostgreSQL、SQLite 或 SQL Server
3. **🔴 Redis 缓存集成** - 提供高性能缓存支持
4. **👥 用户包管理** - 用户可以自行上传和管理自己的包
5. **🛡️ 基于角色的权限控制** - 支持管理员、用户等不同权限级别

## 🔧 配置说明

### 1. OIDC 提供商配置

在 `appsettings.json` 中配置 OAuth 提供商：

```json
{
  "Authentication": {
    "OIDC": {
      "Enabled": true,
      "Providers": {
        "GitHub": {
          "Enabled": true,
          "ClientId": "your-github-client-id",
          "ClientSecret": "your-github-client-secret",
          "CallbackPath": "/signin-github",
          "Scope": [ "user:email" ]
        },
        "Google": {
          "Enabled": true,
          "ClientId": "your-google-client-id",
          "ClientSecret": "your-google-client-secret",
          "CallbackPath": "/signin-google",
          "Scope": [ "openid", "profile", "email" ]
        }
      }
    }
  }
}
```

### 2. 数据库配置

#### PostgreSQL 配置
```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Database=Old8LangPackageManager;Username=postgres;Password=Old8Lang123!"
  }
}
```

#### SQLite 配置
```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "SQLiteConnection": "Data Source=packages.db"
  }
}
```

### 3. Redis 缓存配置

```json
{
  "Redis": {
    "Enabled": true,
    "ConnectionString": "localhost:6379",
    "Database": 0,
    "KeyPrefix": "o8pm:"
  }
}
```

## 🚀 快速开始

### 1. 获取 OAuth 凭据

#### GitHub OAuth 应用
1. 访问 https://github.com/settings/applications/new
2. 创建新的 OAuth App
3. 设置 Homepage URL: `http://localhost:3000`
4. 设置 Authorization callback URL: `http://localhost:5000/signin-github`
5. 获取 Client ID 和 Client Secret

#### Google OAuth 凭据
1. 访问 https://console.cloud.google.com/apis/credentials
2. 创建新的 OAuth 2.0 客户端 ID
3. 设置授权重定向 URI: `http://localhost:5000/signin-google`
4. 获取 Client ID 和 Client Secret

### 2. 使用 Docker Compose 部署

```bash
# 启动所有服务
docker-compose up -d

# 查看服务状态
docker-compose ps

# 查看日志
docker-compose logs -f
```

### 3. 本地开发部署

```bash
# 运行部署脚本
./deploy.sh

# 启动服务
dotnet run --project Old8Lang.PackageManager.Server
```

## 🔐 认证流程

### 1. 用户登录

```bash
# 获取可用的认证提供商
GET /api/v1/auth/providers

# 启动外部登录
POST /api/v1/auth/login/{provider}

# 登录回调处理
GET /api/v1/auth/callback
```

### 2. 获取用户信息

```bash
# 获取当前用户信息
GET /api/v1/auth/me
Authorization: Cookie <session_cookie>

# 响应示例
{
  "id": 1,
  "username": "johndoe",
  "email": "john@example.com",
  "displayName": "John Doe",
  "avatarUrl": "https://github.com/johndoe.png",
  "isEmailVerified": true,
  "isAdmin": false,
  "externalLogins": [
    {
      "provider": "GitHub",
      "providerDisplayName": "GitHub",
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

## 📦 用户包管理

### 1. 上传包

```bash
# 上传包（需要用户认证）
POST /v3/package
Authorization: Cookie <session_cookie>
Content-Type: multipart/form-data

language=old8lang
author=Your Name
description=My package
license=MIT
tags=utility,tools
packageFile=@MyPackage.1.0.0.o8pkg
```

### 2. 权限控制

- **`RequireAuthenticatedUser`** - 需要用户登录
- **`RequireAdmin`** - 需要管理员权限
- **`CanUpload`** - 可以上传包的用户

## 🗄️ 数据库迁移

### 创建迁移

```bash
# 创建新的数据库迁移
dotnet ef migrations add MigrationName --project Old8Lang.PackageManager.Server

# 应用迁移
dotnet ef database update --project Old8Lang.PackageManager.Server
```

### 用户管理相关表

- `Users` - 用户基本信息
- `UserExternalLogins` - 外部登录关联
- `RefreshTokens` - 刷新令牌
- `UserSessions` - 用户会话
- `UserRoles` - 用户角色
- `UserActivityLogs` - 用户活动日志

## 🔍 API 接口

### 认证相关

| 方法 | 路径 | 描述 |
|------|------|------|
| `GET` | `/api/v1/auth/me` | 获取当前用户信息 |
| `POST` | `/api/v1/auth/logout` | 用户登出 |
| `GET` | `/api/v1/auth/providers` | 获取可用的认证提供商 |
| `POST` | `/api/v1/auth/login/{provider}` | 启动外部登录 |

### 包管理

| 方法 | 路径 | 描述 |
|------|------|------|
| `POST` | `/v3/package` | 上传包（需要认证） |
| `GET` | `/v3/search` | 搜索包 |
| `GET` | `/v3/package/{id}` | 获取包详情 |
| `GET` | `/v3/package/{id}/{version}/download` | 下载包 |

## 🛡️ 安全特性

1. **OAuth 2.0 / OIDC 认证** - 使用业界标准的认证协议
2. **CSRF 保护** - 跨站请求伪造保护
3. **会话管理** - 安全的用户会话管理
4. **权限控制** - 基于角色的访问控制
5. **API 密钥支持** - 同时支持用户认证和 API 密钥认证

## 🐛 故障排除

### 1. OAuth 回调失败

检查以下配置：
- 回调 URL 是否正确
- Client ID 和 Client Secret 是否有效
- 应用权限范围是否正确

### 2. 数据库连接失败

检查以下项目：
- 数据库连接字符串格式
- 数据库服务是否运行
- 网络连接是否正常

### 3. Redis 连接问题

检查以下内容：
- Redis 服务是否启动
- 连接字符串是否正确
- 防火墙设置

## 📚 更多文档

- [完整 API 文档](http://localhost:5000/swagger)
- [前端开发指南](./frontend/README.md)
- [数据库设计](./docs/database.md)
- [安全最佳实践](./docs/security.md)

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request 来改进这个项目！