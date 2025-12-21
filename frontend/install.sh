#!/bin/bash

# Old8Lang Package Manager Frontend - 安装脚本

set -e

echo "🚀 安装 Old8Lang Package Manager Frontend..."

# 检查 Node.js 是否已安装
if ! command -v node &> /dev/null
then
    echo "❌ 错误: 未找到 Node.js，请先安装 Node.js (推荐 18+ 版本)"
    echo "   下载地址: https://nodejs.org/"
    exit 1
fi

# 检查 Node.js 版本
NODE_VERSION=$(node -v | cut -d'v' -f2)
REQUIRED_NODE_VERSION="18.0.0"

if ! node -e "process.exit(require('semver').gte('$NODE_VERSION', '$REQUIRED_NODE_VERSION') ? 0 : 1)" 2>/dev/null
then
    echo "❌ 错误: Node.js 版本过低，需要 $REQUIRED_NODE_VERSION 或更高版本"
    echo "   当前版本: $NODE_VERSION"
    exit 1
fi

# 检查包管理器
if command -v pnpm &> /dev/null; then
    PACKAGE_MANAGER="pnpm"
    echo "📦 使用 pnpm 包管理器"
elif command -v yarn &> /dev/null; then
    PACKAGE_MANAGER="yarn"
    echo "📦 使用 Yarn 包管理器"
else
    PACKAGE_MANAGER="npm"
    echo "📦 使用 npm 包管理器"
fi

# 安装依赖
echo "📦 安装依赖..."
case $PACKAGE_MANAGER in
    "pnpm")
        pnpm install
        ;;
    "yarn")
        yarn install
        ;;
    *)
        npm install
        ;;
esac

echo "✅ 依赖安装完成！"

# 检查环境变量
if [ ! -f ".env.development" ]; then
    echo "⚠️  警告: 未找到 .env.development 文件"
    echo "   请确保后端服务器地址配置正确"
fi

# 提示启动命令
echo ""
echo "🎉 安装完成！现在可以启动开发服务器："
echo ""
echo "📱 开发服务器:"
echo "   $PACKAGE_MANAGER run dev"
echo ""
echo "🏗️ 构建生产版本:"
echo "   $PACKAGE_MANAGER run build"
echo ""
echo "🔍 预览构建结果:"
echo "   $PACKAGE_MANAGER run preview"
echo ""
echo "🧪 运行测试:"
echo "   $PACKAGE_MANAGER run test"
echo ""
echo "📋 代码检查:"
echo "   $PACKAGE_MANAGER run lint"
echo ""
echo "⚙️  配置说明:"
echo "   - 后端 API 地址请在 .env.development 中配置"
echo "   - 默认代理后端地址: http://localhost:5000"
echo ""