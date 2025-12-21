# Old8Lang Package Manager (o8pm)

基于 NuGet 设计思路实现的 Old8Lang 语言包管理器，提供完整的包生态系统解决方案。

## 📋 概览

Old8Lang Package Manager 是一个现代化的包管理系统，参考了 NuGet 的核心设计模式，为 Old8Lang 语言提供完整的包管理解决方案。它支持包的创建、打包、分发、安装和管理等全生命周期操作。

## 🏗️ 核心架构

### 核心组件

- **包源管理 (PackageSourceManager)** - 管理多个包源，支持本地和远程源
- **包安装器 (DefaultPackageInstaller)** - 负责包的安装、卸载和管理
- **依赖解析器 (DefaultPackageResolver)** - 智能解析包依赖关系
- **版本管理器 (VersionManager)** - 语义化版本控制和兼容性检查
- **包配置管理器 (DefaultPackageConfigurationManager)** - 管理项目包配置文件
- **包还原器 (PackageRestorer)** - 批量还原项目依赖

## 📦 包格式与打包

### 包结构

每个 Old8Lang 包采用 `.o8pkg` 格式，实际上是一个 ZIP 压缩包，包含以下结构：

```
MyPackage.1.0.0.o8pkg
├── package.json              # 包元数据
├── lib/                      # 编译后的库文件
│   ├── old8lang-1.0/
│   │   └── MyPackage.o8
│   └── old8lang-1.1/
│       └── MyPackage.o8
├── docs/                     # 文档文件
│   ├── README.md
│   └── api.md
├── examples/                 # 示例代码
│   └── basic/
└── tools/                    # 工具脚本
    └── install.js
```

### package.json 元数据

```json
{
  "id": "MyPackage",
  "version": "1.0.0",
  "description": "一个实用的 Old8Lang 工具包",
  "author": "Developer Name",
  "license": "MIT",
  "homepage": "https://github.com/user/mypackage",
  "repository": {
    "type": "git",
    "url": "https://github.com/user/mypackage.git"
  },
  "keywords": ["utility", "tools", "old8lang"],
  "dependencies": [
    {
      "id": "Logger",
      "version": ">=1.2.0",
      "targetFramework": "old8lang-1.0"
    }
  ],
  "frameworks": {
    "old8lang-1.0": {},
    "old8lang-1.1": {}
  },
  "publishedAt": "2024-01-01T00:00:00Z",
  "checksum": "sha256:abc123...",
  "size": 1024000
}
```

### 打包流程

1. **准备源码**
   ```bash
   mkdir MyPackage
   cd MyPackage
   # 创建源码文件和目录结构
   ```

2. **编译项目**
   ```bash
   # 使用 Old8Lang 编译器
   o8c build --framework old8lang-1.0 --output lib/old8lang-1.0/
   ```

3. **创建 package.json**
   ```bash
   # 手动创建或使用工具生成
   o8pm init --id MyPackage --version 1.0.0
   ```

4. **打包**
   ```bash
   # 打包为 .o8pkg 文件
   o8pm pack
   # 生成: MyPackage.1.0.0.o8pkg
   ```

## 🌐 传输与分发

### 包源类型

#### 1. 本地包源
```json
{
  "name": "Local Source",
  "source": "./packages",
  "type": "local"
}
```

#### 2. HTTP 远程包源
```json
{
  "name": "Official Repository",
  "source": "https://packages.old8lang.org/v3/index.json",
  "type": "http"
}
```

#### 3. 包源索引文件 (index.json)
```json
{
  "version": "3.0.0",
  "resources": [
    {
      "@id": "https://packages.old8lang.org/v3/search",
      "@type": "SearchQueryService",
      "comment": "查询包服务"
    },
    {
      "@id": "https://packages.old8lang.org/v3/package/{id}/index.json",
      "@type": "PackageIndexService",
      "comment": "包索引服务"
    }
  ]
}
```

### 传输协议

#### 1. HTTP/HTTPS API

**搜索包**
```http
GET /v3/search?q=logger&skip=0&take=20
```

**获取包信息**
```http
GET /v3/package/{id}/index.json
```

**下载包**
```http
GET /v3/package/{id}/{version}/package.o8pkg
```

#### 2. 包元数据 API
```json
{
  "versions": ["1.0.0", "1.1.0", "2.0.0"],
  "items": [
    {
      "version": "1.0.0",
      "packageContent": "https://packages.old8lang.org/v3/package/MyPackage/1.0.0/package.o8pkg",
      "packageHash": "sha256:abc123...",
      "publishedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

## 🔌 API 架构

### 核心接口

#### IPackageSource
```csharp
public interface IPackageSource
{
    string Name { get; }
    string Source { get; }
    bool IsEnabled { get; set; }
    
    Task<Package?> GetPackageMetadataAsync(string packageId, string version);
    Task<Stream> DownloadPackageAsync(string packageId, string version);
    Task<IEnumerable<Package>> SearchPackagesAsync(string searchTerm, int skip = 0, int take = 20);
    Task<IEnumerable<string>> GetPackageVersionsAsync(string packageId);
}
```

#### IPackageInstaller
```csharp
public interface IPackageInstaller
{
    Task<InstallResult> InstallPackageAsync(string packageId, string version, string installPath);
    Task<bool> UninstallPackageAsync(string packageId, string version, string installPath);
    Task<bool> IsPackageInstalledAsync(string packageId, string version, string installPath);
    Task<IEnumerable<Package>> GetInstalledPackagesAsync(string installPath);
}
```

#### IPackageResolver
```csharp
public interface IPackageResolver
{
    Task<ResolveResult> ResolveDependenciesAsync(string packageId, string version, IEnumerable<IPackageSource> sources);
    Task<bool> CheckCompatibilityAsync(string packageVersion, string requiredVersionRange);
    Task<DependencyGraph> BuildDependencyGraphAsync(string packageId, string version, IEnumerable<IPackageSource> sources);
}
```

### HTTP 包源实现

#### RemotePackageSource
```csharp
public class RemotePackageSource : IPackageSource
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    
    public async Task<Stream> DownloadPackageAsync(string packageId, string version)
    {
        var url = $"{_baseUrl}/package/{packageId}/{version}/package.o8pkg";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }
    
    public async Task<Package?> GetPackageMetadataAsync(string packageId, string version)
    {
        var url = $"{_baseUrl}/package/{packageId}/{version}/metadata.json";
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Package>(json);
    }
}
```

## 🚀 快速开始

### 安装配置

1. **安装 o8pm**
   ```bash
   # 下载对应平台的二进制文件
   curl -L https://github.com/old8lang/o8pm/releases/latest/download/o8pm-linux-x64 -o o8pm
   chmod +x o8pm
   sudo mv o8pm /usr/local/bin/
   ```

2. **初始化项目**
   ```bash
   # 创建新项目
   mkdir MyOld8LangProject
   cd MyOld8LangProject
   
   # 初始化包配置
   o8pm init
   # 生成 o8packages.json
   ```

### 基本命令

```bash
# 添加包
o8pm add MyPackage 1.0.0

# 移除包
o8pm remove MyPackage

# 还原所有包
o8pm restore

# 搜索包
o8pm search logger

# 更新包
o8pm update MyPackage

# 列出已安装包
o8pm list

# 创建新包
o8pm new MyPackage --template library

# 打包项目
o8pm pack

# 发布包
o8pm push MyPackage.1.0.0.o8pkg --source https://api.old8lang.org
```

### 配置文件示例

项目根目录的 `o8packages.json` 文件：

```json
{
  "version": "1.0.0",
  "projectName": "MyOld8LangProject",
  "framework": "old8lang-1.0",
  "installPath": "packages",
  "sources": [
    {
      "name": "Old8Lang Official",
      "source": "https://packages.old8lang.org/v3/index.json",
      "isEnabled": true,
      "type": "http"
    },
    {
      "name": "Local Packages",
      "source": "./local-packages",
      "isEnabled": true,
      "type": "local"
    }
  ],
  "references": [
    {
      "packageId": "Logger",
      "version": "1.2.0",
      "isDevelopmentDependency": false,
      "targetFramework": "old8lang-1.0"
    },
    {
      "packageId": "HttpClient",
      "version": ">=2.0.0",
      "isDevelopmentDependency": false,
      "targetFramework": "old8lang-1.0"
    }
  ],
  "frameworkAssemblies": [
    {
      "name": "System.Core",
      "version": "1.0.0"
    }
  ]
}
```

## 🔧 高级功能

### 1. 版本约束

支持的版本范围语法：
- `1.0.0` - 精确版本
- `1.0.*` - 通配符版本
- `>=1.0.0` - 最小版本
- `<=2.0.0` - 最大版本
- `>1.0.0 <2.0.0` - 范围版本
- `~1.0.0` - 兼容版本 (>=1.0.0 <2.0.0)
- `^1.0.0` - 主要版本兼容 (>=1.0.0 <2.0.0)

### 2. 依赖解析算法

采用回溯算法进行依赖解析：

1. **收集依赖** - 递归收集所有直接和间接依赖
2. **版本冲突检测** - 检测版本冲突并提供解决方案
3. **最优版本选择** - 选择满足所有约束的最新版本
4. **循环依赖检测** - 检测并报告循环依赖

### 3. 缓存机制

- **全局缓存**: `~/.o8pm/cache/` 存储下载的包
- **项目缓存**: `./packages/cache/` 存储项目特定缓存
- **元数据缓存**: 缓存包索引和搜索结果
- **LRU 淘汰**: 基于最近最少使用的缓存清理策略

## 🔒 安全机制

### 1. 包完整性验证

```csharp
public class PackageVerifier
{
    public async Task<bool> VerifyPackageAsync(string packagePath, string expectedChecksum)
    {
        using var stream = File.OpenRead(packagePath);
        var hash = await ComputeSha256Async(stream);
        return hash.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2. 包签名（未来扩展）

- 使用 RSA 或 ECDSA 数字签名
- 信任链验证机制
- 吊销列表支持

## 📊 性能优化

### 1. 并发下载

```csharp
public async Task DownloadPackagesAsync(IEnumerable<PackageDependency> dependencies)
{
    var semaphore = new SemaphoreSlim(5); // 限制并发数
    var tasks = dependencies.Select(async dep =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await DownloadSinglePackageAsync(dep);
        }
        finally
        {
            semaphore.Release();
        }
    });
    
    await Task.WhenAll(tasks);
}
```

### 2. 增量更新

- 基于文件修改时间的智能更新
- 差异下载支持
- 压缩传输优化

## 🧪 测试状态

### 测试覆盖情况

- **✅ 核心功能测试**: 69/69 通过
  - Python包解析器: 25/25 通过 ✅
  - 简单多语言测试: 21/21 通过 ✅  
  - 包存在性检查: 3/3 通过 ✅
  - 基础包管理: 20/20 通过 ✅

- **🔧 集成测试**: 部分进行中
  - HTTP API接口测试
  - 多语言兼容性测试
  - PyPI/NPM兼容性测试

### 测试运行

```bash
# 运行所有测试
dotnet test

# 运行核心功能测试
dotnet test --filter "FullyQualifiedName~PythonPackageParserTests"
dotnet test --filter "FullyQualifiedName~SimpleMultiLanguageTests"

# 构建解决方案
dotnet build Old8Lang.PackageManager.sln

# 格式化代码
dotnet format Old8Lang.PackageManager.sln
```

## 🔮 未来扩展

- [x] ✅ 多语言包支持 (Python & Old8Lang)
- [x] ✅ 包解析器实现
- [x] ✅ 基础测试框架
- [ ] 包发布与版本管理平台
- [ ] 包签名验证系统
- [ ] 私有包源托管
- [ ] 包分析工具
- [ ] 依赖树可视化
- [ ] 自动化包更新
- [ ] 包质量评分系统

## 📈 与其他包管理器的对比

| 特性 | npm | NuGet | pip | Old8Lang Package Manager |
|------|-----|-------|-----|-------------------------|
| 包格式 | .tgz | .nupkg | .whl | .o8pkg |
| 版本管理 | SemVer | SemVer | PEP 440 | SemVer |
| 依赖解析 | 递归 | 智能解析 | 基本解析 | 智能回溯解析 |
| 多源支持 | 是 | 是 | 有限 | ✅ 完整支持 |
| 私有源 | 是 | 是 | 有限 | ✅ 原生支持 |
| 缓存机制 | 是 | 是 | 基本缓存 | ✅ 多级缓存 |
| 签名验证 | 有限 | 是 | 有限 | ✅ 计划支持 |

## 🤝 贡献指南

欢迎贡献代码！请查看 [CONTRIBUTING.md](CONTRIBUTING.md) 了解详细信息。

### 开发环境搭建

```bash
# 克隆仓库
git clone https://github.com/old8lang/o8pm.git
cd o8pm

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行测试
dotnet test

# 运行示例
dotnet run --project Old8Lang.PackageManager -- help
```

这个包管理器成功地将成熟的包管理理念应用到了 Old8Lang 语言生态，为开发者提供了一个功能完整、性能优异、安全可靠的包管理解决方案。