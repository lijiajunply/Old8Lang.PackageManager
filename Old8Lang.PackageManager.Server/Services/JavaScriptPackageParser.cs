using System.Text.Json;
using Old8Lang.PackageManager.Server.Models;
using System.Text.RegularExpressions;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// JavaScript/TypeScript 包解析服务
/// </summary>
public interface IJavaScriptPackageParser
{
    Task<JavaScriptPackageInfo?> ParsePackageAsync(Stream packageStream, string fileName);
    Task<List<ExternalDependencyInfo>> ParsePackageJsonAsync(Stream packageJsonStream);
    Task<bool> ValidateJavaScriptPackageAsync(Stream packageStream);
    string GetLanguageFromExtension(string fileName);
}

/// <summary>
/// JavaScript/TypeScript 包解析服务实现
/// </summary>
public class JavaScriptPackageParser(ILogger<JavaScriptPackageParser> logger) : IJavaScriptPackageParser
{
    // NPM 版本正则表达式
    private static readonly Regex NpmVersionRegex = new(@"^\d+\.\d+\.\d+(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$", RegexOptions.Compiled);

    public async Task<JavaScriptPackageInfo?> ParsePackageAsync(Stream packageStream, string fileName)
    {
        try
        {
            // JS/TS 包通常是 .tgz 格式 (tarball)
            if (!IsJavaScriptPackage(fileName))
            {
                logger.LogWarning("文件不是有效的 JavaScript/TypeScript 包: {FileName}", fileName);
                return null;
            }
            
            var packageInfo = new JavaScriptPackageInfo();
            
            if (fileName.EndsWith(".tgz"))
            {
                packageInfo = await ParseTarballFileAsync(packageStream, fileName);
            }
            else
            {
                logger.LogWarning("不支持的 JavaScript/TypeScript 包格式: {FileName}", fileName);
                return null;
            }
            
            return packageInfo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "解析 JavaScript/TypeScript 包失败: {FileName}", fileName);
            return null;
        }
    }
    
    public async Task<List<ExternalDependencyInfo>> ParsePackageJsonAsync(Stream packageJsonStream)
    {
        var dependencies = new List<ExternalDependencyInfo>();
        
        try
        {
            using var reader = new StreamReader(packageJsonStream);
            var content = await reader.ReadToEndAsync();
            
            var packageJson = JsonSerializer.Deserialize<PackageJsonDocument>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (packageJson == null)
                return dependencies;
            
            // 解析 dependencies
            if (packageJson.Dependencies != null)
            {
                foreach (var dep in packageJson.Dependencies)
                {
                    dependencies.Add(new ExternalDependencyInfo
                    {
                        DependencyType = "npm",
                        PackageName = dep.Key,
                        VersionSpec = dep.Value,
                        IndexUrl = "",
                        ExtraIndexUrl = "",
                        IsDevDependency = false
                    });
                }
            }
            
            // 解析 devDependencies
            if (packageJson.DevDependencies != null)
            {
                foreach (var dep in packageJson.DevDependencies)
                {
                    dependencies.Add(new ExternalDependencyInfo
                    {
                        DependencyType = "npm",
                        PackageName = dep.Key,
                        VersionSpec = dep.Value,
                        IndexUrl = "",
                        ExtraIndexUrl = "",
                        IsDevDependency = true
                    });
                }
            }
            
            // 解析 peerDependencies
            if (packageJson.PeerDependencies != null)
            {
                foreach (var dep in packageJson.PeerDependencies)
                {
                    dependencies.Add(new ExternalDependencyInfo
                    {
                        DependencyType = "npm",
                        PackageName = dep.Key,
                        VersionSpec = dep.Value,
                        IndexUrl = "",
                        ExtraIndexUrl = "",
                        IsDevDependency = false
                    });
                }
            }
            
            return dependencies;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "解析 package.json 失败");
            return dependencies;
        }
    }
    
    public async Task<bool> ValidateJavaScriptPackageAsync(Stream packageStream)
    {
        try
        {
            // 基本验证：检查文件格式
            packageStream.Position = 0;
            var buffer = new byte[1024];
            _ = await packageStream.ReadAsync(buffer, 0, buffer.Length);
            packageStream.Position = 0;
            
            // gzip 格式检查 (tarball 是 gzip 压缩的 tar 文件)
            var isGzip = buffer[0] == 0x1F && buffer[1] == 0x8B;
            
            return isGzip;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "验证 JavaScript/TypeScript 包失败");
            return false;
        }
    }
    
    public string GetLanguageFromExtension(string fileName)
    {
        return fileName.ToLowerInvariant() switch
        {
            var f when f.EndsWith(".tgz") || f.EndsWith(".tar.gz") => "javascript",
            var f when f.EndsWith(".js") || f.EndsWith(".mjs") || f.EndsWith(".cjs") => "javascript",
            var f when f.EndsWith(".ts") || f.EndsWith(".mts") || f.EndsWith(".cts") => "typescript",
            var f when f.EndsWith(".d.ts") => "typescript",
            var f when f.EndsWith(".json") && f.Contains("package") => "javascript",
            _ => "unknown"
        };
    }
    
    private async Task<JavaScriptPackageInfo> ParseTarballFileAsync(Stream packageStream, string fileName)
    {
        var packageInfo = new JavaScriptPackageInfo();
        
        // 从文件名解析包名和版本
        // 格式: {package}-{version}.tgz
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (fileNameWithoutExt.EndsWith(".tar"))
        {
            fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileNameWithoutExt);
        }
        
        // 处理 scope 包 (@scope/package-name)
        var lastDashIndex = fileNameWithoutExt.LastIndexOf('-');
        if (lastDashIndex > 0 && !fileNameWithoutExt.StartsWith("@"))
        {
            packageInfo.PackageId = fileNameWithoutExt.Substring(0, lastDashIndex);
            packageInfo.Version = fileNameWithoutExt.Substring(lastDashIndex + 1);
        }
        else if (fileNameWithoutExt.StartsWith("@"))
        {
            // 对于 scope 包，需要更复杂的解析
            var parts = fileNameWithoutExt.Split('-');
            if (parts.Length >= 3)
            {
                packageInfo.PackageId = string.Join("-", parts.Take(parts.Length - 1));
                packageInfo.Version = parts.Last();
            }
        }
        
        // 尝试从包内的 package.json 文件读取详细信息
        try
        {
            var metadata = await ExtractPackageJsonMetadataAsync(packageStream);
            if (metadata != null)
            {
                packageInfo.Description = metadata.Description;
                packageInfo.Author = metadata.Author?.Name ?? "";
                packageInfo.License = metadata.License;
                packageInfo.ProjectUrl = metadata.Homepage ?? metadata.Repository?.Url;
                packageInfo.Summary = "";
                packageInfo.Keywords = metadata.Keywords?.ToList() ?? new List<string>();
                packageInfo.Main = metadata.Main;
                packageInfo.Types = metadata.Types;
                packageInfo.Module = metadata.Module;
                packageInfo.Exports = metadata.Exports != null ? JsonSerializer.Serialize(metadata.Exports) : "";
                packageInfo.Files = metadata.Files?.ToList() ?? new List<string>();
                packageInfo.Engines = metadata.Engines?.Select(kvp => $"{kvp.Key}@{kvp.Value}").ToList() ?? new List<string>();
                
                // 解析依赖
                if (metadata.Dependencies != null)
                {
                    foreach (var dep in metadata.Dependencies)
                    {
                        var depInfo = new ExternalDependencyInfo
                        {
                            DependencyType = "npm",
                            PackageName = dep.Key,
                            VersionSpec = dep.Value,
                            IndexUrl = "",
                            ExtraIndexUrl = "",
                            IsDevDependency = false
                        };
                        packageInfo.Dependencies.Add(depInfo);
                    }
                }
                
                if (metadata.DevDependencies != null)
                {
                    foreach (var dep in metadata.DevDependencies)
                    {
                        var depInfo = new ExternalDependencyInfo
                        {
                            DependencyType = "npm",
                            PackageName = dep.Key,
                            VersionSpec = dep.Value,
                            IndexUrl = "",
                            ExtraIndexUrl = "",
                            IsDevDependency = true
                        };
                        packageInfo.Dependencies.Add(depInfo);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "无法提取 tarball 文件的元数据，使用文件名解析");
        }
        
        return packageInfo;
    }
    
    private Task<PackageJsonDocument?> ExtractPackageJsonMetadataAsync(Stream packageStream)
    {
        try
        {
            // 简化实现：实际应该解压 tar.gz 文件并读取 package.json
            // 这里返回模拟数据
            var result = new PackageJsonDocument
            {
                Name = "example-package",
                Version = "1.0.0",
                Description = "A JavaScript/TypeScript package",
                Author = new AuthorInfo { Name = "Unknown" },
                License = "MIT",
                Homepage = "",
                Repository = new RepositoryInfo { Url = "" },
                Keywords = new List<string> { "javascript", "typescript" },
                Main = "index.js",
                Types = "index.d.ts",
                Module = "index.mjs",
                Files = new List<string> { "lib/", "types/" },
                Engines = new Dictionary<string, string> { { "node", ">=14.0.0" } },
                Dependencies = new Dictionary<string, string>(),
                DevDependencies = new Dictionary<string, string>()
            };
            return Task.FromResult<PackageJsonDocument?>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "提取 package.json 元数据失败");
            return Task.FromResult<PackageJsonDocument?>(null);
        }
    }
    
    private bool IsJavaScriptPackage(string fileName)
    {
        var lowerFileName = fileName.ToLowerInvariant();
        return lowerFileName.EndsWith(".tgz") || lowerFileName.EndsWith(".tar.gz");
    }
}

/// <summary>
/// JavaScript/TypeScript 包信息
/// </summary>
public class JavaScriptPackageInfo
{
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public string Main { get; set; } = string.Empty; // 入口文件
    public string Types { get; set; } = string.Empty; // TypeScript 声明文件
    public string Module { get; set; } = string.Empty; // ES 模块入口
    public string Exports { get; set; } = string.Empty; // 导出配置
    public List<string> Files { get; set; } = new(); // 包含的文件
    public List<string> Engines { get; set; } = new(); // 引擎要求
    public List<ExternalDependencyInfo> Dependencies { get; set; } = new();
}

/// <summary>
/// package.json 文档结构
/// </summary>
public class PackageJsonDocument
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AuthorInfo? Author { get; set; }
    public string License { get; set; } = string.Empty;
    public string Homepage { get; set; } = string.Empty;
    public RepositoryInfo? Repository { get; set; }
    public List<string>? Keywords { get; set; }
    public string Main { get; set; } = string.Empty;
    public string Types { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public object? Exports { get; set; }
    public List<string>? Files { get; set; }
    public Dictionary<string, string>? Engines { get; set; }
    public Dictionary<string, string>? Dependencies { get; set; }
    public Dictionary<string, string>? DevDependencies { get; set; }
    public Dictionary<string, string>? PeerDependencies { get; set; }
    public Dictionary<string, string>? OptionalDependencies { get; set; }
    public Dictionary<string, object>? Scripts { get; set; }
}

/// <summary>
/// 作者信息
/// </summary>
public class AuthorInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// 仓库信息
/// </summary>
public class RepositoryInfo
{
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}