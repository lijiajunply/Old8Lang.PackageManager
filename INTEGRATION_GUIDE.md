# Old8Lang.PackageManager.Core 集成指南

本文档说明如何将 `Old8Lang.PackageManager.Core` NuGet 包集成到 Old8Lang 或其他脚本语言项目中。

## 📦 NuGet 包信息

- **包名**: `Old8Lang.PackageManager.Core`
- **版本**: 1.0.0
- **目标框架**: .NET 10.0
- **许可证**: MIT

## 🚀 安装

### 方式 1：通过 NuGet.org（发布后）

```bash
dotnet add package Old8Lang.PackageManager.Core
```

### 方式 2：通过本地 NuGet 包

```bash
# 添加本地 NuGet 源
dotnet nuget add source /path/to/Old8Lang.PackageManager/Old8Lang.PackageManager.Core/nupkg --name LocalPackages

# 安装包
dotnet add package Old8Lang.PackageManager.Core --version 1.0.0 --source LocalPackages
```

### 方式 3：直接引用本地 .nupkg 文件

在项目的 `.csproj` 文件中添加：

```xml
<ItemGroup>
  <PackageReference Include="Old8Lang.PackageManager.Core" Version="1.0.0">
    <!-- 指定本地包的路径 -->
    <Source>/path/to/Old8Lang.PackageManager/Old8Lang.PackageManager.Core/nupkg</Source>
  </PackageReference>
</ItemGroup>
```

## 🔧 集成到 Old8Lang

### 步骤 1：修改 Old8Lang 项目引用

将 Old8Lang 项目中对 `Old8Lang.PackageManager.Core` 的项目引用改为 NuGet 包引用。

**之前** (Old8Lang/Old8Lang.csproj):
```xml
<ItemGroup>
  <ProjectReference Include="..\..\Old8Lang.PackageManager\Old8Lang.PackageManager.Core\Old8Lang.PackageManager.Core.csproj" />
</ItemGroup>
```

**之后**:
```xml
<ItemGroup>
  <PackageReference Include="Old8Lang.PackageManager.Core" Version="1.0.0" />
</ItemGroup>
```

### 步骤 2：更新代码引用（如需要）

Core 库的命名空间保持不变，大部分代码不需要修改：

```csharp
using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Services;
using Old8Lang.PackageManager.Core.Models;
```

### 步骤 3：使用语言适配器

如果要使用通用化功能，可以使用或继承 `Old8LangAdapter`:

```csharp
using Old8Lang.PackageManager.Core.Adapters;
using Old8Lang.PackageManager.Core.Interfaces;

// 使用内置的 Old8Lang 适配器
var adapter = new Old8LangAdapter();

// 或创建自定义适配器
public class CustomOld8LangAdapter : ILanguageAdapter
{
    public string LanguageName => "old8lang";
    // ... 实现接口
}
```

## 🌐 集成到其他脚本语言

### 示例：为 Python 脚本语言创建适配器

```csharp
using Old8Lang.PackageManager.Core.Interfaces;

public class PythonAdapter : ILanguageAdapter
{
    public string LanguageName => "python";

    public IEnumerable<string> SupportedFileExtensions => new[] { ".py", ".pyw" };

    public string ConfigurationFileName => "requirements.txt";

    public bool ValidatePackageFormat(string packagePath)
    {
        // 检查是否为有效的 Python 包
        return Directory.Exists(packagePath) &&
               (File.Exists(Path.Combine(packagePath, "setup.py")) ||
                File.Exists(Path.Combine(packagePath, "pyproject.toml")));
    }

    public async Task<PackageMetadata?> ExtractMetadataAsync(string packagePath)
    {
        // 从 setup.py 或 pyproject.toml 提取元数据
        // ...
    }

    public Task OnPackageInstalledAsync(string packagePath)
    {
        // Python 包安装后的操作
        Console.WriteLine($"[Python] Package installed: {packagePath}");
        return Task.CompletedTask;
    }

    public Task OnPackageUninstallingAsync(string packagePath)
    {
        Console.WriteLine($"[Python] Uninstalling package: {packagePath}");
        return Task.CompletedTask;
    }
}
```

### 使用示例

```csharp
using Old8Lang.PackageManager.Core.Services;
using Old8Lang.PackageManager.Core.Interfaces;

var projectRoot = "/path/to/python/project";
var adapter = new PythonAdapter();

// 创建包管理器组件
var sourceManager = new PackageSourceManager();
sourceManager.AddSource(new LocalPackageSource("local", "./packages"));

var resolver = new DefaultPackageResolver();
var installer = new DefaultPackageInstaller(projectRoot, resolver);

// 安装包
var installPath = Path.Combine(projectRoot, "packages");
var result = await installer.InstallPackageAsync("requests", "2.28.2", installPath);

if (result.Success)
{
    // 调用适配器的回调
    await adapter.OnPackageInstalledAsync(Path.Combine(installPath, "requests"));
    Console.WriteLine("Package installed successfully!");
}
```

## 📚 核心 API 使用

### 1. 包源管理

```csharp
using Old8Lang.PackageManager.Core.Services;

var sourceManager = new PackageSourceManager();

// 添加本地包源
sourceManager.AddSource(new LocalPackageSource(
    name: "Local Packages",
    source: "./local-packages"
));

// 获取所有包源
var sources = sourceManager.GetAllSources();
```

### 2. 依赖解析

```csharp
using Old8Lang.PackageManager.Core.Services;

var resolver = new DefaultPackageResolver();

// 解析包依赖
var result = await resolver.ResolveDependenciesAsync(
    packageId: "MyPackage",
    version: "1.0.0",
    sources: sourceManager.GetAllSources()
);

if (result.Success)
{
    foreach (var dep in result.ResolvedDependencies)
    {
        Console.WriteLine($"Dependency: {dep.PackageId} {dep.VersionRange}");
    }
}
```

### 3. 包安装

```csharp
using Old8Lang.PackageManager.Core.Services;

var installer = new DefaultPackageInstaller(projectRoot, resolver);

var installResult = await installer.InstallPackageAsync(
    packageId: "Logger",
    version: "1.2.0",
    installPath: Path.Combine(projectRoot, "packages")
);

if (installResult.Success)
{
    Console.WriteLine($"Installed: {installResult.InstalledPackage?.Id}");
}
```

### 4. 包配置管理

```csharp
using Old8Lang.PackageManager.Core.Services;
using Old8Lang.PackageManager.Core.Models;

var configManager = new DefaultPackageConfigurationManager();
var configPath = Path.Combine(projectRoot, "o8packages.json");

// 读取配置
var config = await configManager.ReadConfigurationAsync(configPath);

// 添加包引用
await configManager.AddPackageReferenceAsync(
    configPath,
    packageId: "HttpClient",
    version: "2.0.0"
);

// 保存配置
await configManager.WriteConfigurationAsync(configPath, config);
```

## 🎯 集成架构示例

### Old8Lang 集成架构

```
Old8Lang 项目
│
├── Old8Lang (核心库)
│   ├── PackageManagement/
│   │   └── PackageManager.cs          # 运行时包加载器
│   └── AST/Statement/
│       └── ImportStatement.cs         # 导入语句处理
│
└── [NuGet 依赖]
    └── Old8Lang.PackageManager.Core   # 包管理核心功能
        ├── IPackageSource             # 包源接口
        ├── IPackageResolver           # 依赖解析
        ├── IPackageInstaller          # 包安装
        └── ILanguageAdapter           # 语言适配

使用流程:
1. Old8Lang 代码: import "Logger"
2. ImportStatement 解析导入
3. PackageManager 查找本地包
4. (可选) 使用 Core 的依赖解析和安装功能
```

### 通用脚本语言集成架构

```
自定义脚本语言项目
│
├── MyScriptLanguage/
│   ├── Interpreter/
│   │   └── ImportHandler.cs          # 导入处理
│   └── PackageManagement/
│       ├── MyLanguageAdapter.cs      # 自定义适配器
│       └── PackageLoader.cs          # 包加载器
│
└── [NuGet 依赖]
    └── Old8Lang.PackageManager.Core
```

## ⚙️ 配置选项

### PackageConfiguration (o8packages.json)

```json
{
  "Version": "1.0.0",
  "ProjectName": "MyProject",
  "Framework": "old8lang-1.0",
  "InstallPath": "packages",
  "Sources": [
    {
      "Name": "Local Packages",
      "Source": "./local-packages",
      "IsEnabled": true
    },
    {
      "Name": "Official Repository",
      "Source": "https://packages.old8lang.org",
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

## 🔌 自定义包源

实现 `IPackageSource` 接口创建自定义包源：

```csharp
using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;

public class HttpPackageSource : IPackageSource
{
    public string Name { get; }
    public string Source { get; }
    public bool IsEnabled { get; }

    private readonly HttpClient _httpClient;

    public HttpPackageSource(string name, string baseUrl)
    {
        Name = name;
        Source = baseUrl;
        IsEnabled = true;
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<Package?> GetPackageMetadataAsync(string packageId, string version)
    {
        var response = await _httpClient.GetAsync($"/api/packages/{packageId}/{version}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Package>(json);
    }

    public async Task<IEnumerable<Package>> SearchPackagesAsync(string query, bool includePrerelease = false)
    {
        var response = await _httpClient.GetAsync($"/api/search?q={query}");
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Package>>(json) ?? [];
    }

    public async Task<Stream> DownloadPackageAsync(string packageId, string version)
    {
        var response = await _httpClient.GetAsync($"/api/packages/{packageId}/{version}/download");
        return await response.Content.ReadAsStreamAsync();
    }

    // ... 实现其他方法
}
```

## 📖 更多资源

- [NuGet 包 README](https://www.nuget.org/packages/Old8Lang.PackageManager.Core/)
- [GitHub 仓库](https://github.com/old8lang/o8pm)
- [Old8Lang 语言文档](https://github.com/old8lang/old8lang)

## 💬 支持

如有问题，请：
- 提交 [GitHub Issue](https://github.com/old8lang/o8pm/issues)
- 查看 [文档](https://github.com/old8lang/o8pm/blob/main/README.md)

---

**使用 Old8Lang.PackageManager.Core，让您的脚本语言拥有强大的包管理能力！** 🚀
