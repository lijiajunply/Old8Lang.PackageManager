# Old8Lang.PackageManager.Core

[![NuGet](https://img.shields.io/nuget/v/Old8Lang.PackageManager.Core.svg)](https://www.nuget.org/packages/Old8Lang.PackageManager.Core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

通用的包管理器核心库，提供包解析、依赖管理、版本控制等核心功能。设计为**语言无关的通用框架**，可被任何脚本语言项目使用来实现包管理功能。

## ✨ 主要特性

- 🧩 **智能依赖解析** - 支持复杂依赖关系的回溯算法，自动解决依赖冲突
- 📦 **多源管理** - 支持本地源、远程源、私有源的统一管理
- 🔢 **版本控制** - 完整的语义化版本（SemVer）支持和版本约束
- 🔌 **扩展性强** - 基于接口设计，易于扩展和定制
- 🌐 **语言无关** - 可被任何编程语言的包管理系统使用

## 🚀 快速开始

### 安装

```bash
dotnet add package Old8Lang.PackageManager.Core
```

### 基本使用

```csharp
using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Services;

// 1. 创建包源管理器
var sourceManager = new PackageSourceManager(projectRoot);

// 2. 创建包解析器
var resolver = new DefaultPackageResolver();

// 3. 创建包安装器
var installer = new DefaultPackageInstaller(projectRoot, resolver);

// 4. 添加包
await installer.InstallPackageAsync("MyPackage", "1.2.0");

// 5. 解析依赖
var result = await resolver.ResolveDependenciesAsync(
    "MyPackage",
    "1.2.0",
    sourceManager.GetAllSources()
);

if (result.Success)
{
    Console.WriteLine("依赖解析成功！");
    foreach (var dep in result.ResolvedDependencies)
    {
        Console.WriteLine($"  - {dep.PackageId} {dep.VersionRange}");
    }
}
```

## 📖 核心概念

### 1. 包源 (IPackageSource)

包源是包的来源，可以是：
- **本地文件系统** - 本地目录中的包
- **远程服务器** - HTTP/HTTPS 包仓库
- **私有源** - 企业内部包仓库

```csharp
public interface IPackageSource
{
    string Name { get; }
    string Source { get; }
    bool IsEnabled { get; }

    Task<Package?> GetPackageMetadataAsync(string packageId, string version);
    Task<IEnumerable<Package>> SearchPackagesAsync(string query);
    Task<Stream> DownloadPackageAsync(string packageId, string version);
}
```

### 2. 包解析器 (IPackageResolver)

负责依赖关系解析和版本兼容性检查：

```csharp
public interface IPackageResolver
{
    Task<ResolveResult> ResolveDependenciesAsync(
        string packageId,
        string version,
        IEnumerable<IPackageSource> sources
    );

    bool IsVersionCompatible(string requestedVersion, string availableVersion);
    VersionRange ParseVersionRange(string versionRange);
}
```

### 3. 包安装器 (IPackageInstaller)

负责包的安装、卸载和管理：

```csharp
public interface IPackageInstaller
{
    Task<bool> InstallPackageAsync(string packageId, string version);
    Task<bool> UninstallPackageAsync(string packageId);
    Task<bool> UpdatePackageAsync(string packageId, string newVersion);
    Task RestorePackagesAsync();
}
```

### 4. 配置管理器 (IPackageConfigurationManager)

管理项目的包配置文件：

```csharp
public interface IPackageConfigurationManager
{
    PackageConfiguration Load(string configPath);
    void Save(string configPath, PackageConfiguration config);
    void AddReference(string configPath, string packageId, string version);
    void RemoveReference(string configPath, string packageId);
}
```

## 🔧 高级用法

### 自定义包源

实现 `IPackageSource` 接口创建自定义包源：

```csharp
public class MyCustomPackageSource : IPackageSource
{
    public string Name => "My Custom Source";
    public string Source => "https://my-packages.example.com";
    public bool IsEnabled => true;

    public async Task<Package?> GetPackageMetadataAsync(string packageId, string version)
    {
        // 实现自定义的包元数据获取逻辑
        var response = await httpClient.GetAsync($"{Source}/api/packages/{packageId}/{version}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Package>(json);
    }

    // 实现其他方法...
}
```

### 依赖解析示例

```csharp
var resolver = new DefaultPackageResolver();
var sources = new List<IPackageSource>
{
    new LocalPackageSource("/path/to/packages"),
    new MyCustomPackageSource()
};

var result = await resolver.ResolveDependenciesAsync(
    packageId: "MyApp",
    version: "1.0.0",
    sources: sources
);

if (!result.Success)
{
    Console.WriteLine($"解析失败: {result.Message}");
    foreach (var conflict in result.Conflicts)
    {
        Console.WriteLine($"  冲突: {conflict}");
    }
}
```

### 版本约束

支持以下版本约束语法：

```csharp
var versionManager = new VersionManager();

// 精确版本
versionManager.IsVersionCompatible("1.2.3", "1.2.3"); // true

// 范围版本
versionManager.IsVersionCompatible(">=1.0.0", "1.5.0"); // true
versionManager.IsVersionCompatible("^1.2.0", "1.9.0"); // true
versionManager.IsVersionCompatible("~1.2.0", "1.2.5"); // true

// 通配符
versionManager.IsVersionCompatible("1.2.*", "1.2.8"); // true
```

## 📚 使用场景

### 1. 为脚本语言构建包管理器

```csharp
// 为您的脚本语言项目添加包管理功能
public class MyScriptLanguagePackageManager
{
    private readonly IPackageInstaller installer;
    private readonly IPackageResolver resolver;

    public MyScriptLanguagePackageManager(string projectRoot)
    {
        var sourceManager = new PackageSourceManager(projectRoot);
        resolver = new DefaultPackageResolver();
        installer = new DefaultPackageInstaller(projectRoot, resolver);
    }

    public async Task<bool> AddPackage(string name, string version)
    {
        return await installer.InstallPackageAsync(name, version);
    }

    public async Task<bool> RemovePackage(string name)
    {
        return await installer.UninstallPackageAsync(name);
    }
}
```

### 2. 私有包仓库系统

```csharp
// 构建企业内部的包管理服务
public class EnterprisePackageService
{
    private readonly PackageSourceManager sourceManager;

    public EnterprisePackageService()
    {
        sourceManager = new PackageSourceManager("/var/packages");

        // 添加企业私有源
        sourceManager.AddSource(new PackageSourceInfo
        {
            Name = "Enterprise Private",
            Source = "https://packages.company.internal",
            IsEnabled = true
        });
    }
}
```

### 3. 依赖管理工具

```csharp
// 创建依赖分析和管理工具
public class DependencyAnalyzer
{
    private readonly IPackageResolver resolver;

    public async Task<List<PackageDependency>> AnalyzeDependencies(
        string packageId,
        string version)
    {
        var result = await resolver.ResolveDependenciesAsync(
            packageId,
            version,
            GetAllSources()
        );

        return result.ResolvedDependencies;
    }
}
```

## 🏗️ 架构设计

```
Old8Lang.PackageManager.Core
├── Interfaces/              # 核心接口定义
│   ├── IPackageSource       # 包源接口
│   ├── IPackageResolver     # 解析器接口
│   ├── IPackageInstaller    # 安装器接口
│   └── IPackageConfigurationManager
├── Services/                # 默认实现
│   ├── LocalPackageSource   # 本地包源
│   ├── DefaultPackageResolver
│   ├── DefaultPackageInstaller
│   ├── PackageSourceManager
│   └── VersionManager       # 版本管理
├── Models/                  # 数据模型
│   ├── Package              # 包模型
│   ├── PackageConfiguration # 配置模型
│   └── PackageDependency    # 依赖模型
└── Commands/                # 命令模式
    └── PackageCommands      # 包管理命令
```

## 🔌 扩展点

该库设计了多个扩展点，方便定制：

1. **自定义包源** - 实现 `IPackageSource`
2. **自定义解析器** - 实现 `IPackageResolver`
3. **自定义安装器** - 实现 `IPackageInstaller`
4. **自定义版本逻辑** - 继承 `VersionManager`
5. **自定义配置格式** - 实现 `IPackageConfigurationManager`

## 🤝 贡献

欢迎贡献！请查看 [GitHub 仓库](https://github.com/old8lang/o8pm) 了解更多信息。

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](https://github.com/old8lang/o8pm/blob/main/LICENSE)

## 🔗 相关链接

- [NuGet 包](https://www.nuget.org/packages/Old8Lang.PackageManager.Core/)
- [GitHub 仓库](https://github.com/old8lang/o8pm)
- [问题反馈](https://github.com/old8lang/o8pm/issues)
- [Old8Lang 语言](https://github.com/old8lang/old8lang)

## 💬 支持

如有问题或建议，请：
- 提交 [GitHub Issue](https://github.com/old8lang/o8pm/issues)
- 加入讨论 [GitHub Discussions](https://github.com/old8lang/o8pm/discussions)

---

**使用 Old8Lang.PackageManager.Core，为您的项目添加强大的包管理能力！** 🎉
