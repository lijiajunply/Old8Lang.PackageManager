#!/bin/bash

# Old8Lang Package Manager Server 部署脚本

set -e

echo "🚀 开始部署 Old8Lang Package Manager Server..."

# 检查 .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: 未找到 .NET SDK，请先安装 .NET 10.0 SDK"
    exit 1
fi

# 进入项目目录
cd "$(dirname "$0")"

echo "📦 还原依赖..."
dotnet restore

echo "🔨 构建项目..."
dotnet build --configuration Release

echo "🗄️ 初始化数据库..."
# 根据配置选择数据库类型
DB_TYPE=$(jq -r '.DatabaseProvider // "SQLite"' Old8Lang.PackageManager.Server/appsettings.json 2>/dev/null || echo "SQLite")

case "$DB_TYPE" in
  "PostgreSQL")
    echo "🐘 使用 PostgreSQL 数据库"
    # 检查 PostgreSQL 连接
    if ! command -v psql &> /dev/null; then
        echo "⚠️  警告: 未找到 psql，请确保 PostgreSQL 已安装并可访问"
    fi
    dotnet ef database update --project Old8Lang.PackageManager.Server --context PackageManagerDbContext
    ;;
  "SQLServer")
    echo "🗄️ 使用 SQL Server 数据库"
    dotnet ef database update --project Old8Lang.PackageManager.Server --context PackageManagerDbContext
    ;;
  *)
    echo "🗄️ 使用 SQLite 数据库"
    dotnet ef database update --project Old8Lang.PackageManager.Server --context PackageManagerDbContext
    ;;
esac

echo "🎉 部署完成！"

echo ""
echo "📋 启动说明:"
echo "  开发模式:  dotnet run --project Old8Lang.PackageManager.Server"
echo "  生产模式:  dotnet run --project Old8Lang.PackageManager.Server --configuration Release"
echo ""
echo "🌐 API 文档:  http://localhost:5000/swagger"
echo "🔍 健康检查:  http://localhost:5000/health"
echo "🔐 认证接口:  http://localhost:5000/api/v1/auth"
echo ""
echo "⚙️  配置文件: Old8Lang.PackageManager.Server/appsettings.json"
echo "📦 包存储路径: packages/ (可配置)"
echo ""
echo "🔑 OAuth 配置:"
echo "  请在 appsettings.json 中配置 GitHub、Google 等 OAuth 提供商的 ClientId 和 ClientSecret"
echo "  GitHub: https://github.com/settings/applications/new"
echo "  Google: https://console.cloud.google.com/apis/credentials"
echo ""
echo "🐳 Docker 部署:"
echo "  docker-compose up -d"
echo ""
echo "🌍 多语言包管理:"
echo "  支持 Old8Lang、Python、JavaScript/TypeScript 包"
echo "  用户可自行上传和管理自己的包"
echo ""