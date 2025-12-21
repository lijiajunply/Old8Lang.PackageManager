# Old8Lang Package Manager 前端项目

基于 Vue 3 + Vite + Tailwind CSS + Naive UI 构建的现代化包管理器前端界面。

## 🎯 项目概览

这个前端项目为 Old8Lang 包管理器提供了完整的 Web 界面，包括：

- 🏠 **主页** - 展示热门包和快速搜索入口
- 🔍 **搜索页面** - 多维度包搜索和筛选
- 📦 **包详情页** - 详细的包信息和下载功能
- ⬆️ **上传页面** - 包上传和管理
- 📚 **文档中心** - 完整的 API 文档和使用指南
- 🚫 **404 页面** - 优雅的错误处理

## 🚀 快速启动

### 1. 安装依赖

```bash
# 使用安装脚本（推荐）
./install.sh

# 或手动安装
npm install
```

### 2. 启动开发服务器

```bash
npm run dev
```

访问 http://localhost:3000

### 3. 构建生产版本

```bash
npm run build
```

## 🛠️ 技术架构

```
┌─────────────────────────────────────────┐
│              Vue 3 + TypeScript            │
├─────────────────────────────────────────┤
│                 Vite                   │
├─────────────────────────────────────────┤
│              Tailwind CSS              │
├─────────────────────────────────────────┤
│                Naive UI               │
├─────────────────────────────────────────┤
│               Pinia                   │
├─────────────────────────────────────────┤
│            Vue Router                 │
├─────────────────────────────────────────┤
│              Axios                    │
└─────────────────────────────────────────┘
```

## 🎨 设计系统

### 主题支持
- 🌞 浅色主题（默认）
- 🌙 深色主题（自动检测系统偏好）

### 响应式设计
- 📱 移动端优化
- 💻 桌面端完整体验
- 📱 平板端适配

### 组件库
- 使用 Naive UI 提供一致的视觉体验
- 自定义 Tailwind CSS 样式系统
- 流畅的过渡动画和微交互

## 🔗 API 集成

### 代理配置
开发环境下自动代理后端 API：
- `/api/*` → `http://localhost:5000/api/*`
- `/simple/*` → `http://localhost:5000/simple/*`
- `/pypi/*` → `http://localhost:5000/pypi/*`
- `/npm/*` → `http://localhost:5000/npm/*`

### 兼容性
- ✅ 原生 API 接口
- ✅ PyPI 兼容接口
- ✅ NPM 兼容接口

## 📁 项目结构

```
frontend/
├── src/
│   ├── api/                    # API 客户端封装
│   ├── assets/                 # 静态资源
│   │   ├── images/
│   │   └── styles/
│   ├── components/             # 可复用组件
│   ├── router/                 # 路由配置
│   ├── stores/                 # Pinia 状态管理
│   ├── types/                  # TypeScript 类型定义
│   ├── utils/                  # 工具函数
│   ├── views/                  # 页面组件
│   ├── App.vue                 # 根组件
│   └── main.ts                 # 应用入口
├── public/                     # 公共文件
├── dist/                      # 构建输出
└── 配置文件...
```

## 🎯 核心功能

### 🔍 智能搜索
- **多语言支持** - Old8Lang、Python、JavaScript、TypeScript
- **智能筛选** - 按版本、大小、更新时间等
- **搜索历史** - 记录最近搜索
- **实时建议** - 热门关键词推荐

### 📦 包管理
- **详细信息展示** - 版本、依赖、下载统计
- **一键下载** - 支持 `.o8pkg` 格式
- **安装命令** - 自动生成安装命令
- **相关推荐** - 智能推荐相关包

### ⬆️ 包发布
- **拖拽上传** - 支持文件拖拽
- **进度显示** - 实时上传进度
- **格式验证** - 包格式完整性检查
- **批量操作** - 支持批量上传管理

## 🌟 特色功能

### 🎨 用户体验
- **流畅动画** - 页面切换和加载动画
- **骨架屏** - 优雅的加载状态
- **错误处理** - 友好的错误提示
- **快捷操作** - 键盘快捷键支持

### 📱 移动端优化
- **触摸友好** - 适配触摸操作
- **手势支持** - 滑动、缩放手势
- **响应式布局** - 自适应各种屏幕
- **PWA 支持** - 可安装为桌面应用

## 🔧 开发工具

### 代码质量
- **TypeScript** - 完整类型检查
- **ESLint** - 代码规范检查
- **Prettier** - 代码格式化
- **Stylelint** - CSS 规范检查

### 开发体验
- **热更新** - 开发时实时更新
- **HMR** - 模块热替换
- **Source Map** - 便于调试
- **开发工具** - Vue DevTools 集成

## 🚀 部署指南

### Docker 部署

```bash
# 构建镜像
docker build -t o8pm-frontend .

# 运行容器
docker run -p 3000:80 o8pm-frontend
```

### Nginx 配置

```nginx
server {
    listen 80;
    server_name your-domain.com;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://backend:5000/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## 🔮 未来规划

- [ ] **PWA 功能** - 离线支持和桌面安装
- [ ] **国际化** - 多语言界面支持
- [ ] **主题定制** - 更多颜色和布局选项
- [ ] **数据分析** - 包使用统计和趋势
- [ ] **社交功能** - 包评价、评论、收藏
- [ ] **CI/CD 集成** - 自动化部署流水线

## 🤝 贡献指南

欢迎贡献代码！请遵循以下步骤：

1. Fork 项目仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🙏 致谢

感谢以下开源项目：
- [Vue.js](https://vuejs.org/) - 渐进式 JavaScript 框架
- [Vite](https://vitejs.dev/) - 下一代前端构建工具
- [Naive UI](https://www.naiveui.com/) - 优秀的 Vue 3 组件库
- [Tailwind CSS](https://tailwindcss.com/) - 实用优先的 CSS 框架

---

**开始使用 Old8Lang Package Manager Frontend！** 🎉

如果遇到问题，请查看 [常见问题](./FAQ.md) 或提交 [Issue](https://github.com/old8lang/o8pm-frontend/issues)。