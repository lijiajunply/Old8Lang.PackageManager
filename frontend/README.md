# Old8Lang Package Manager Frontend

基于 Vue 3 + Vite + Tailwind CSS + Naive UI 构建的现代化包管理器前端界面。

## 🚀 快速开始

### 环境要求

- Node.js 18.0+ 
- pnpm 8.0+ (推荐) 或 npm/yarn

### 安装依赖

```bash
# 使用安装脚本（推荐）
./install.sh

# 或手动安装
pnpm install
# 或
npm install
```

### 开发服务器

```bash
pnpm run dev
# 或
npm run dev
```

访问 http://localhost:3000

### 构建生产版本

```bash
pnpm run build
# 或
npm run build
```

### 预览构建结果

```bash
pnpm run preview
# 或
npm run preview
```

## 📁 项目结构

```
frontend/
├── src/
│   ├── api/              # API 客户端
│   ├── assets/            # 静态资源
│   │   ├── images/
│   │   └── styles/
│   ├── components/        # Vue 组件
│   ├── router/            # 路由配置
│   ├── stores/            # Pinia 状态管理
│   ├── types/             # TypeScript 类型定义
│   ├── utils/             # 工具函数
│   ├── views/             # 页面组件
│   │   ├── HomeView.vue       # 首页
│   │   ├── SearchView.vue     # 搜索页面
│   │   ├── PackageView.vue   # 包详情页面
│   │   ├── UploadView.vue    # 上传页面
│   │   ├── DocsView.vue      # 文档页面
│   │   └── NotFoundView.vue  # 404 页面
│   ├── App.vue            # 根组件
│   └── main.ts            # 入口文件
├── public/                    # 公共文件
├── .env                       # 环境变量
├── .env.development           # 开发环境变量
├── index.html                 # HTML 模板
├── package.json              # 项目配置
├── tailwind.config.js        # Tailwind CSS 配置
├── vite.config.ts            # Vite 配置
└── tsconfig.json             # TypeScript 配置
```

## 🛠️ 技术栈

- **Vue 3** - 渐进式 JavaScript 框架
- **Vite** - 下一代前端构建工具
- **TypeScript** - 类型安全的 JavaScript
- **Naive UI** - Vue 3 组件库
- **Tailwind CSS** - 实用优先的 CSS 框架
- **Pinia** - Vue 状态管理库
- **Vue Router** - Vue 官方路由管理器
- **Axios** - HTTP 客户端

## 🎨 设计特性

- 🌗 响应式设计，支持桌面和移动设备
- 🌓 深色/浅色主题切换
- 🌍 多语言支持（中文/英文）
- 🎯 现代化 UI 组件设计
- ⚡ 流畅的页面过渡和动画
- 🔍 智能搜索和筛选功能
- 📱 移动端友好的交互设计

## 📦 主要功能

### 🏠 首页
- 包搜索入口
- 热门包展示
- 快速访问链接
- 功能特性介绍

### 🔍 搜索页面
- 多维度搜索（名称、描述、关键词）
- 语言筛选（Old8Lang、Python、JavaScript、TypeScript）
- 排序选项（相关性、名称、创建时间、下载量）
- 搜索历史记录
- 分页浏览结果

### 📦 包详情页面
- 包基本信息展示
- 版本管理和下载
- 依赖关系图表
- 关键词标签
- 下载统计数据
- 相关包推荐

### ⬆️ 上传页面
- 拖拽文件上传
- 上传进度显示
- 包信息配置
- 格式验证
- API 密钥认证

### 📚 文档页面
- API 参考文档
- 开发指南
- 示例代码
- 最佳实践

## 🔧 开发指南

### 代码规范

使用 ESLint + TypeScript 进行代码检查：

```bash
# 运行代码检查
pnpm run lint

# 运行样式检查
pnpm run lint:style
```

### 环境变量配置

```bash
# .env.development
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_TITLE=Old8Lang Package Manager
```

### API 代理配置

开发环境下，Vite 会自动代理以下请求到后端：

- `/api/*` → `http://localhost:5000/api/*`
- `/simple/*` → `http://localhost:5000/simple/*`
- `/pypi/*` → `http://localhost:5000/pypi/*`
- `/npm/*` → `http://localhost:5000/npm/*`

## 🎯 组件使用

### Naive UI 组件

```vue
<template>
  <n-button type="primary" @click="handleClick">
    按钮文本
  </n-button>
  
  <n-input v-model:value="inputValue" placeholder="输入..." />
  
  <n-card title="卡片标题">
    卡片内容
  </n-card>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const inputValue = ref('')
const handleClick = () => {
  console.log('按钮被点击')
}
</script>
```

### Tailwind CSS 样式

```vue
<template>
  <div class="bg-white rounded-lg shadow-md p-6">
    <h2 class="text-xl font-bold text-gray-900 mb-4">
      标题
    </h2>
    <p class="text-gray-600 leading-relaxed">
      内容文本
    </p>
  </div>
</template>
```

## 🚀 部署

### 构建生产版本

```bash
pnpm run build
```

### 环境变量

```bash
# 生产环境变量
VITE_API_BASE_URL=https://your-api-domain.com/api
VITE_APP_TITLE=Old8Lang Package Manager
```

### Docker 部署

```dockerfile
FROM node:18-alpine AS builder

WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production

COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

## 🔍 故障排除

### 常见问题

1. **端口冲突**
   ```bash
   # 修改 vite.config.ts 中的端口
   server: {
     port: 3001  // 改为其他端口
   }
   ```

2. **API 连接失败**
   ```bash
   # 检查后端服务是否启动
   curl http://localhost:5000/api/packages/search
   
   # 检查环境变量配置
   cat .env.development
   ```

3. **依赖安装失败**
   ```bash
   # 清除缓存重新安装
   rm -rf node_modules
   rm package-lock.json
   npm install
   ```

### 开发工具

推荐使用以下编辑器和扩展：

- **VS Code** + Vue Language Features (Volar)
- **WebStorm** + Vue.js Plugin
- **TypeScript** 和 **ESLint** 集成

## 🤝 贡献

1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](../LICENSE) 文件了解详情。