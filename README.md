# Old8Lang Package Manager (o8pm)

基于 NuGet 设计思路实现的 Old8Lang 语言包管理器。

## 📋 概览

这个包管理器参考了 NuGet 的核心设计模式，为 Old8Lang 语言提供完整的包管理解决方案。

### 🏗️ 核心架构

- **包源管理 (PackageSourceManager)** - 管理多个包源，支持本地和远程源
- **包安装器 (DefaultPackageInstaller)** - 负责包的安装、卸载和管理
- **依赖解析器 (DefaultPackageResolver)** - 智能解析包依赖关系
- **版本管理器 (VersionManager)** - 语义化版本控制和兼容性检查
- **包配置管理器 (DefaultPackageConfigurationManager)** - 管理项目包配置文件
- **包还原器 (PackageRestorer)** - 批量还原项目依赖

## 🚀 快速开始

### 基本命令

```bash
# 显示帮助
o8pm help

# 添加包
o8pm add MyPackage 1.0.0

# 移除包
o8pm remove MyPackage

# 还原所有包
o8pm restore

# 搜索包
o8pm search logger
```

### 配置文件

项目根目录的 `o8packages.json` 文件：

```json
{
  "Version": "1.0.0",
  "ProjectName": "MyOld8LangProject",
  "Framework": "old8lang-1.0",
  "InstallPath": "packages",
  "Sources": [
    {
      "Name": "Old8Lang Official",
      "Source": "https://packages.old8lang.org/v3/index.json",
      "IsEnabled": true
    },
    {
      "Name": "Local Packages",
      "Source": "./local-packages",
      "IsEnabled": true
    }
  ],
  "References": [
    {
      "PackageId": "Logger",
      "Version": "1.2.0",
      "IsDevelopmentDependency": false,
      "TargetFramework": "old8lang-1.0"
    }
  ]
}
```

## 📦 项目结构

```
Old8Lang.PackageManager/
├── Old8Lang.PackageManager.Core/
│   ├── Interfaces/          # 核心接口定义
│   │   ├── IPackageSource.cs
│   │   ├── IPackageInstaller.cs
│   │   ├── IPackageResolver.cs
│   │   └── IPackageConfigurationManager.cs
│   ├── Models/             # 数据模型
│   │   ├── Package.cs
│   │   └── PackageConfiguration.cs
│   ├── Services/           # 核心服务实现
│   │   ├── PackageSourceManager.cs
│   │   ├── DefaultPackageInstaller.cs
│   │   ├── DefaultPackageResolver.cs
│   │   ├── LocalPackageSource.cs
│   │   ├── PackageRestorer.cs
│   │   ├── DefaultPackageConfigurationManager.cs
│   │   └── VersionManager.cs
│   └── Commands/           # CLI 命令
│       └── PackageCommands.cs
└── Old8Lang.PackageManager/  # CLI 应用程序
    └── Program.cs
```

## 🔧 核心功能

### 1. 多源支持
- **官方源**: https://packages.old8lang.org/v3/index.json
- **本地源**: 支持本地文件系统包源
- **自定义源**: 可配置任意数量的包源

### 2. 智能依赖解析
- 递归解析依赖关系
- 版本兼容性检查
- 循环依赖检测
- 版本范围支持

### 3. 版本管理
- 语义化版本控制 (SemVer)
- 版本范围语法支持:
  - `1.0.0` - 精确版本
  - `1.0.*` - 通配符版本
  - `>=1.0.0` - 最小版本
  - `1.0.0-2.0.0` - 版本范围

### 4. 包存储
- 本地包缓存
- 元数据管理
- 校验和验证
- 按版本组织存储

## 📋 命令参考

| 命令 | 描述 | 示例 |
|------|------|------|
| `add` | 添加包到项目 | `o8pm add MyPackage 1.0.0` |
| `remove` | 从项目移除包 | `o8pm remove MyPackage` |
| `restore` | 还原所有包 | `o8pm restore` |
| `search` | 搜索包 | `o8pm search logger` |
| `help` | 显示帮助 | `o8pm help` |

## 🎯 设计亮点

### 1. 模块化设计
- 清晰的接口分离
- 可插拔的组件架构
- 易于扩展和测试

### 2. 异步操作
- 所有 I/O 操作异步化
- 支持并发操作
- 高性能包管理

### 3. 错误处理
- 详细的错误信息
- 警告和错误分离
- 优雅的降级处理

### 4. 配置驱动
- JSON 配置文件
- 灵活的源管理
- 项目级别的定制

## 🔮 未来扩展

- [ ] 远程包源实现 (HTTP/HTTPS)
- [ ] 包发布功能
- [ ] 包签名验证
- [ ] 包缓存优化
- [ ] 全局配置管理
- [ ] 包更新检查
- [ ] 依赖树可视化

## 🏆 与 NuGet 的对比

| 特性 | NuGet | Old8Lang Package Manager |
|------|-------|-------------------------|
| 核心架构 | 是 | ✅ 相同设计模式 |
| 多源支持 | 是 | ✅ 支持 |
| 版本管理 | SemVer | ✅ SemVer 兼容 |
| 依赖解析 | 智能解析 | ✅ 智能解析 |
| 配置文件 | project.json/packages.config | ✅ JSON 格式 |
| CLI 接口 | 丰富命令 | ✅ 核心命令 |
| 缓存机制 | 是 | ✅ 本地缓存 |

这个包管理器成功地将 NuGet 的核心设计理念应用到了 Old8Lang 语言，提供了一个功能完整、架构清晰的包管理解决方案。