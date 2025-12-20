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
dotnet ef database update --project Old8Lang.PackageManager.Server

echo "🎉 部署完成！"

echo ""
echo "📋 启动说明:"
echo "  开发模式:  dotnet run --project Old8Lang.PackageManager.Server"
echo "  生产模式:  dotnet run --project Old8Lang.PackageManager.Server --configuration Release"
echo ""
echo "🌐 API 文档:  http://localhost:5001/swagger"
echo "💚 健康检查:  http://localhost:5001/health"
echo ""
echo "⚙️  配置文件: Old8Lang.PackageManager.Server/appsettings.json"
echo "📦 包存储路径: packages/ (可配置)"
echo ""