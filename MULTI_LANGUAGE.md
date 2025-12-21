# Old8Lang Package Manager - 多语言支持

## 📖 概述

Old8Lang Package Manager 现已支持多语言包管理，包括 Old8Lang 和 Python 语言。这个扩展使得开发者可以在统一的平台上管理不同语言的依赖包，提供一致的体验。

## 🌐 支持的语言

### Old8Lang (old8lang)
- **包格式**: `.o8pkg`
- **配置文件**: `o8packages.json`
- **依赖解析**: 智能依赖解析算法
- **版本管理**: 语义化版本控制 (SemVer)

### Python (python)
- **包格式**: `.whl` (wheel), `.tar.gz` (源码包)
- **配置文件**: `requirements.txt`, `pyproject.toml`
- **PyPI 兼容**: 完全兼容 PyPI API
- **依赖管理**: pip, conda 支持

### JavaScript/TypeScript (javascript/typescript)
- **包格式**: `.tgz`, `.tar.gz` (npm tarball)
- **配置文件**: `package.json`
- **NPM 兼容**: 完全兼容 NPM Registry API
- **依赖管理**: npm, yarn, pnpm 支持

## 🚀 快速开始

### 配置多语言包源

在 `o8packages.json` 中配置多语言包源：

```json
{
  "version": "1.0.0",
  "projectName": "MyMultiLangProject",
  "sources": [
    {
      "name": "Old8Lang Official",
      "source": "https://packages.old8lang.org/v3/index.json",
      "isEnabled": true,
      "languages": ["old8lang"]
    },
    {
      "name": "Python Packages",
      "source": "https://packages.old8lang.org/simple",
      "isEnabled": true,
      "languages": ["python"]
    },
    {
      "name": "JavaScript/TypeScript Packages",
      "source": "https://packages.old8lang.org/npm",
      "isEnabled": true,
      "languages": ["javascript", "typescript"]
    }
  ],
  "references": [
    {
      "packageId": "MyOld8LangPackage",
      "version": "1.0.0",
      "language": "old8lang"
    },
    {
      "packageId": "requests",
      "version": ">=2.28.0",
      "language": "python"
    },
    {
      "packageId": "lodash",
      "version": "^4.17.21",
      "language": "javascript"
    }
  ]
}
```

### 搜索不同语言的包

```bash
# 搜索所有语言的包
o8pm search "utility"

# 搜索特定语言的包
o8pm search "logger" --language old8lang
o8pm search "requests" --language python
o8pm search "utility" --language javascript
o8pm search "types" --language typescript

# 搜索热门包
o8pm popular --language python
o8pm popular --language old8lang
o8pm popular --language javascript
o8pm popular --language typescript
```

### 安装多语言包

```bash
# 安装 Old8Lang 包
o8pm add MyOld8LangPackage 1.0.0

# 安装 Python 包
o8pm add requests==2.28.2 --language python

# 安装 JavaScript/TypeScript 包
o8pm add lodash@^4.17.21 --language javascript
o8pm add typescript@^5.0.0 --language typescript

# 批量安装
o8pm add numpy pandas --language python
o8pm add logger utils --language old8lang
o8pm add lodash moment --language javascript
```

## 📦 JavaScript/TypeScript 包支持

### NPM 兼容 API

服务器提供完整的 NPM 兼容 API，支持：

- **包注册表**: `/npm/` - 注册表信息
- **包信息**: `/npm/{package}` - 包详情
- **包下载**: `/npm/download/{package}/-/{package}-{version}.tgz` - 文件下载
- **包搜索**: `/npm/-/v1/search?q={query}` - 包搜索
- **包发布**: `PUT /npm/{package}` - 发布包
- **包删除**: `DELETE /npm/{package}/{version}` - 删除包版本

### 配置 npm 使用自定义源

```bash
# 临时使用
npm install lodash --registry https://packages.old8lang.org/npm

# 永久配置
npm config set registry https://packages.old8lang.org/npm

# 使用 .npmrc 文件
echo "registry=https://packages.old8lang.org/npm" > .npmrc

# 配置特定作用域
npm config set @old8lang:registry https://packages.old8lang.org/npm
```

### package.json 示例

```json
{
  "name": "@old8lang/example-package",
  "version": "1.0.0",
  "description": "A JavaScript/TypeScript package for Old8Lang",
  "main": "lib/index.js",
  "types": "lib/index.d.ts",
  "module": "lib/index.mjs",
  "exports": {
    ".": {
      "import": "./lib/index.mjs",
      "require": "./lib/index.js",
      "types": "./lib/index.d.ts"
    }
  },
  "files": [
    "lib/",
    "types/",
    "README.md"
  ],
  "scripts": {
    "build": "tsc",
    "test": "jest",
    "lint": "eslint src/"
  },
  "keywords": [
    "javascript",
    "typescript",
    "old8lang",
    "utility"
  ],
  "author": "Old8Lang Team <team@old8lang.org>",
  "license": "MIT",
  "repository": {
    "type": "git",
    "url": "https://github.com/old8lang/example-package.git"
  },
  "homepage": "https://old8lang.org/packages/example-package",
  "engines": {
    "node": ">=14.0.0",
    "npm": ">=6.0.0"
  },
  "dependencies": {
    "lodash": "^4.17.21"
  },
  "devDependencies": {
    "typescript": "^5.0.0",
    "@types/node": "^18.0.0",
    "jest": "^29.0.0",
    "eslint": "^8.0.0"
  },
  "peerDependencies": {
    "react": ">=16.8.0"
  }
}
```

### TypeScript 支持特性

#### 类型声明文件
- 自动包含 `.d.ts` 文件到包中
- 支持 `types` 和 `typings` 字段
- 兼容 TypeScript 模块解析

#### 构建配置
```json
{
  "compilerOptions": {
    "declaration": true,
    "declarationMap": true,
    "outDir": "./lib",
    "rootDir": "./src",
    "module": "ESNext",
    "target": "ES2020",
    "moduleResolution": "node"
  }
}
```

### 包发布流程

```bash
# 登录到自定义注册表
npm login --registry=https://packages.old8lang.org/npm

# 发布包
npm publish --registry=https://packages.old8lang.org/npm

# 发布带作用域的包
npm publish --access public --registry=https://packages.old8lang.org/npm

# 发布预发布版本
npm publish --tag beta --registry=https://packages.old8lang.org/npm
```

### 包管理器兼容性

#### npm
```bash
npm install lodash --registry=https://packages.old8lang.org/npm
npm install @old8lang/example-package
```

#### yarn
```bash
yarn config set registry https://packages.old8lang.org/npm
yarn add lodash
yarn add @old8lang/example-package
```

#### pnpm
```bash
pnpm config set registry https://packages.old8lang.org/npm
pnpm add lodash
pnpm add @old8lang/example-package
```

## 📦 Python 包支持

### PyPI 兼容 API

服务器提供完整的 PyPI 兼容 API，支持：

- **简单索引**: `/simple/` - 包列表
- **包版本**: `/simple/{package}/` - 版本列表  
- **包下载**: `/simple/{package}/{filename}` - 文件下载
- **JSON API**: `/simple/pypi/{package}/json` - 包信息
- **搜索**: `/simple/search?q={query}` - 包搜索

### 配置 pip 使用自定义源

```bash
# 临时使用
pip install requests --index-url https://packages.old8lang.org/simple

# 永久配置
pip config set global.index-url https://packages.old8lang.org/simple

# 配置 requirements.txt 安装
pip install -r requirements.txt --index-url https://packages.old8lang.org/simple
```

### requirements.txt 示例

```txt
# 使用自定义包源
--index-url https://packages.old8lang.org/simple

# 标准包
requests>=2.28.0
numpy==1.21.0
pandas>=1.3.0,<2.0.0

# 开发依赖
pytest>=6.0.0
black>=21.0.0

# 额外的包源
--extra-index-url https://pypi.org/simple
```

## 🔧 API 接口

### 多语言搜索

```http
GET /v3/search?q=utility&language=python&skip=0&take=20
```

**参数**:
- `q`: 搜索关键词
- `language`: 语言筛选 (`old8lang`, `python`)
- `skip`: 跳过数量
- `take`: 获取数量

**响应**:
```json
{
  "totalHits": 42,
  "data": [
    {
      "packageId": "requests",
      "version": "2.28.2",
      "language": "python",
      "description": "HTTP library for Python",
      "author": "Kenneth Reitz",
      "tags": ["http", "web", "api"],
      "publishedAt": "2024-01-15T10:30:00Z",
      "downloadCount": 15000,
      "isPrerelease": false
    }
  ]
}
```

### 多语言包详情

```http
GET /v3/package/{id}?version=1.0.0&language=python
```

**响应**:
```json
{
  "packageId": "requests",
  "version": "2.28.2",
  "language": "python",
  "description": "HTTP library for Python",
  "author": "Kenneth Reitz",
  "license": "Apache 2.0",
  "projectUrl": "https://requests.readthedocs.io/",
  "tags": ["http", "web", "api"],
  "dependencies": [
    {
      "packageId": "urllib3",
      "versionRange": ">=1.21.1,<1.27",
      "isRequired": true
    }
  ],
  "externalDependencies": [
    {
      "dependencyType": "pip",
      "packageName": "certifi",
      "versionSpec": ">=2017.4.17",
      "indexUrl": "",
      "extraIndexUrl": "",
      "isDevDependency": false
    }
  ],
  "languageMetadata": {
    "python": "{\"requires_python\": \">=3.7\", \"classifiers\": [\"Development Status :: 5 - Production/Stable\"]}"
  },
  "publishedAt": "2024-01-15T10:30:00Z",
  "downloadCount": 15000,
  "size": 587840,
  "isPrerelease": false
}
```

### 包上传

```http
POST /v3/package
Content-Type: multipart/form-data

language=python
author=Your Name
description=My Python package
license=MIT
tags=python,utility
externalDependencies[0].dependencyType=pip
externalDependencies[0].packageName=requests
externalDependencies[0].versionSpec>=2.28.0
languageMetadata={"requires_python": ">=3.8"}
packageFile=@my-package-1.0.0-py3-none-any.whl
```

## 🐍 Python 包管理

### 支持的包格式

1. **Wheel (.whl)**
   - 二进制分发格式
   - 快速安装
   - 包含编译后的代码

2. **Source Distribution (.tar.gz)**
   - 源码分发格式
   - 需要编译
   - 跨平台兼容

### 包元数据解析

自动从以下文件提取元数据：
- `METADATA` (wheel)
- `PKG-INFO` (source)
- `pyproject.toml` (现代 Python 包)

### 依赖管理

支持多种依赖类型：
- **运行时依赖**: `requires_dist`
- **构建依赖**: `build_requires`
- **可选依赖**: `extras_require`
- **开发依赖**: `dev_requires`

## 🔍 包搜索功能

### 智能搜索

- **包名匹配**: 精确和模糊匹配
- **关键词搜索**: 描述、标签、作者
- **语言筛选**: 按语言类别筛选
- **版本筛选**: 稳定版 vs 预发布版

### 搜索示例

```bash
# 基础搜索
o8pm search "http client"

# 语言特定搜索
o8pm search "http client" --language python
o8pm search "http client" --language old8lang

# 高级搜索
o8pm search "utility" --prerelease --language python
o8pm search "data" --skip=20 --take=10
```

## 📊 统计和分析

### 多语言统计

```http
GET /api/v1/statistics?language=python
```

**响应**:
```json
{
  "totalPackages": 1250,
  "totalDownloads": 150000,
  "languageBreakdown": {
    "python": 800,
    "old8lang": 450
  },
  "popularPackages": [
    {
      "packageId": "requests",
      "language": "python",
      "downloads": 15000
    }
  ]
}
```

### 下载趋势

```http
GET /api/v1/statistics/downloads/trend?language=python&days=30
```

## 🔒 安全和验证

### Python 包验证

- **格式验证**: 检查 wheel/tar.gz 格式
- **元数据验证**: 验证包信息完整性
- **依赖检查**: 验证依赖关系合法性
- **签名验证**: 可选的数字签名支持

### 安全扫描

计划中的功能：
- **恶意代码检测**: 静态代码分析
- **漏洞扫描**: 依赖漏洞检查
- **许可证检查**: 开源许可证验证

## 📋 CLI 命令扩展

### 多语言命令

```bash
# 列出支持的语言
o8pm languages

# 按语言列出包
o8pm list --language python
o8pm list --language old8lang

# 混合项目初始化
o8pm init --languages python,old8lang

# 语言特定操作
o8pm add numpy --language python
o8pm add MyPackage --language old8lang

# 批量安装
o8pm install requirements.txt --language python
o8pm install o8packages.json --language old8lang
```

### 配置管理

```bash
# 设置默认语言
o8pm config set default-language python

# 语言特定配置
o8pm config set python.index-url https://packages.old8lang.org/simple
o8pm config set old8lang.source https://packages.old8lang.org/v3/index.json
```

## 🚀 部署配置

### 服务器配置

```json
{
  "PackageStorage": {
    "StoragePath": "packages",
    "LanguagePaths": {
      "python": "packages/python",
      "old8lang": "packages/old8lang"
    }
  },
  "Api": {
    "SupportedLanguages": ["python", "old8lang"],
    "DefaultLanguage": "old8lang"
  },
  "PyPI": {
    "Enabled": true,
    "BaseUrl": "https://packages.old8lang.org/simple",
    "RedirectToPyPI": true
  }
}
```

### 环境变量

```bash
# 支持的语言
O8PM_SUPPORTED_LANGUAGES=python,old8lang

# 默认语言
O8PM_DEFAULT_LANGUAGE=old8lang

# PyPI 配置
O8PM_PYPI_ENABLED=true
O8PM_PYPI_REDIRECT_TO_PYPI=true
```

## 🔮 未来扩展

### 计划中的语言支持

- [ ] **Java** - Maven 仓库兼容
- [ ] **Go** - Go modules 兼容
- [ ] **Rust** - Crates.io 兼容
- [ ] **Ruby** - RubyGems 兼容
- [ ] **PHP** - Composer 兼容

### 高级功能

- [ ] **跨语言依赖图** - 可视化语言间依赖
- [ ] **统一依赖解析** - 跨语言依赖冲突检测
- [ ] **多语言 CI/CD** - 自动化构建和发布
- [ ] **包质量评分** - 跨语言质量指标
- [ ] **智能推荐** - 基于使用模式的包推荐

## 🤝 贡献指南

### 添加新语言支持

1. **实现包解析器**
   ```csharp
   public interface ILanguagePackageParser
   {
       Task<PackageInfo?> ParsePackageAsync(Stream packageStream, string fileName);
       string GetLanguageFromExtension(string fileName);
       bool ValidatePackage(Stream packageStream);
   }
   ```

2. **添加语言特定元数据**
   ```csharp
   public class LanguageMetadataEntity
   {
       public string Language { get; set; }
       public string Metadata { get; set; } // JSON 格式
   }
   ```

3. **实现兼容 API** (如需要)
   ```csharp
   [ApiController]
   [Route("npm")]
   public class NpmController : ControllerBase
   ```

4. **更新配置和文档**

## 📚 示例项目

### Python + Old8Lang 混合项目

```
my-mixed-project/
├── python/
│   ├── requirements.txt
│   ├── main.py
│   └── my_python_package/
├── old8lang/
│   ├── o8packages.json
│   ├── main.o8
│   └── my_old8lang_package/
├── docs/
└── README.md
```

### 统一依赖管理

```bash
# 初始化混合项目
o8pm init --languages python,old8lang

# 安装所有依赖
o8pm install

# 运行项目
o8pm run python main.py
o8pm run old8lang main.o8

# 构建项目
o8pm build --all
```

---

多语言支持使 Old8Lang Package Manager 成为一个通用的包管理平台，为开发者提供统一、高效的跨语言依赖管理体验。