# RemotePackageSource 使用指南

## 概述

`RemotePackageSource` 是 `Old8Lang.PackageManager.Core` 中的一个核心组件，用于通过 HTTP API 从远程服务器获取包。它实现了 `IPackageSource` 接口，提供了完整的包管理功能。

## 功能特性

- **HTTP API 集成**: 通过 RESTful API 与远程包服务器通信
- **包搜索**: 支持关键词搜索，包含预发布版本过滤
- **版本管理**: 获取包的所有可用版本
- **包下载**: 支持流式下载包文件
- **元数据获取**: 获取包的详细信息
- **API 认证**: 支持 Bearer Token 认证
- **连接测试**: 提供连接可用性检测
- **错误处理**: 完善的异常处理和错误恢复

## 使用方法

### 基本用法

```csharp
using Old8Lang.PackageManager.Core.Services;

// 创建远程包源
var remoteSource = new RemotePackageSource(
    name: "Official Repository",
    source: "https://packages.example.com",
    apiKey: "your-api-key-here",  // 可选
    timeout: 30                    // 超时时间（秒）
);
```

### 包搜索

```csharp
// 搜索包
var results = await remoteSource.SearchPackagesAsync("logger", includePrerelease: false);
foreach (var package in results)
{
    Console.WriteLine($"{package.Id} v{package.Version} - {package.Description}");
}
```

### 获取包版本

```csharp
// 获取包的所有版本
var versions = await remoteSource.GetPackageVersionsAsync("MyPackage", includePrerelease: false);
Console.WriteLine($"可用版本: {string.Join(", ", versions)}");
```

### 获取包元数据

```csharp
// 获取包详细信息
var package = await remoteSource.GetPackageMetadataAsync("MyPackage", "1.0.0");
if (package != null)
{
    Console.WriteLine($"作者: {package.Author}");
    Console.WriteLine($"描述: {package.Description}");
    Console.WriteLine($"依赖: {package.Dependencies.Count} 个");
}
```

### 下载包

```csharp
// 下载包文件
using var packageStream = await remoteSource.DownloadPackageAsync("MyPackage", "1.0.0");
using var fileStream = File.Create("MyPackage.1.0.0.o8pkg");
await packageStream.CopyToAsync(fileStream);
```

### 连接测试

```csharp
// 测试服务器连接
bool isAvailable = await remoteSource.TestConnectionAsync();
Console.WriteLine(isAvailable ? "服务器可用" : "服务器不可用");
```

## 与包源管理器集成

```csharp
var sourceManager = new PackageSourceManager();

// 添加远程包源
sourceManager.AddSource(remoteSource);

// 通过包源管理器搜索
var allResults = await sourceManager.SearchPackagesAsync("database");
```

## API 兼容性

RemotePackageSource 专门设计用于与 `Old8Lang.PackageManager.Server` 的 API 兼容。支持的 API 端点包括：

- `GET /v3/search` - 包搜索
- `GET /v3/package/{id}` - 获取包信息
- `GET /v3/package/{id}/{version}/download` - 下载包
- `GET /v3/index.json` - 服务索引

## 错误处理

```csharp
try
{
    var results = await remoteSource.SearchPackagesAsync("package");
}
catch (Exception ex)
{
    Console.WriteLine($"搜索失败: {ex.Message}");
}
```

## 资源管理

```csharp
// 使用 using 语句自动释放资源
await using var remoteSource = new RemotePackageSource("Repo", "https://api.example.com");
// 使用包源...
// 资源会自动释放

// 或手动释放
remoteSource.Dispose();
```

## 配置选项

| 参数 | 类型 | 必需 | 默认值 | 说明 |
|------|------|------|--------|------|
| name | string | 是 | - | 包源名称 |
| source | string | 是 | - | 服务器URL |
| apiKey | string? | 否 | null | API密钥 |
| timeout | int | 否 | 30 | 超时时间（秒） |

## 最佳实践

1. **资源管理**: 始终使用 `using` 语句或调用 `Dispose()` 方法释放资源
2. **错误处理**: 包装网络调用在 try-catch 块中
3. **连接测试**: 在使用前先测试连接可用性
4. **API密钥安全**: 不要在代码中硬编码API密钥，使用配置或环境变量
5. **超时设置**: 根据网络环境设置合适的超时时间

## 示例：完整的包安装流程

```csharp
public async Task<bool> InstallPackageFromRemote(string packageId, string version)
{
    var remoteSource = new RemotePackageSource(
        "Official Repo", 
        "https://packages.example.com"
    );

    try
    {
        // 测试连接
        if (!await remoteSource.TestConnectionAsync())
        {
            Console.WriteLine("无法连接到包源");
            return false;
        }

        // 检查包是否存在
        var package = await remoteSource.GetPackageMetadataAsync(packageId, version);
        if (package == null)
        {
            Console.WriteLine($"包 {packageId} v{version} 不存在");
            return false;
        }

        // 下载包
        using var packageStream = await remoteSource.DownloadPackageAsync(packageId, version);
        var fileName = $"{packageId}.{version}.o8pkg";
        using var fileStream = File.Create(fileName);
        await packageStream.CopyToAsync(fileStream);

        Console.WriteLine($"包 {packageId} v{version} 下载成功");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"安装包时出错: {ex.Message}");
        return false;
    }
    finally
    {
        remoteSource.Dispose();
    }
}
```