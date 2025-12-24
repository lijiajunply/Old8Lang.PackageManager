using Old8Lang.PackageManager.Core.Interfaces;
using System.Text.Json;

namespace Old8Lang.PackageManager.Core.Adapters;

/// <summary>
/// Old8Lang 语言适配器示例
/// </summary>
public class Old8LangAdapter : ILanguageAdapter
{
    /// <summary>
    /// 语言名称
    /// </summary>
    public string LanguageName => "old8lang";

    /// <summary>
    /// 支持的文件扩展名
    /// </summary>
    public IEnumerable<string> SupportedFileExtensions => [".o8", ".old8", ".ol"];

    /// <summary>
    /// 包配置文件名
    /// </summary>
    public string ConfigurationFileName => "o8packages.json";

    /// <summary>
    /// 验证包格式
    /// </summary>
    /// <param name="packagePath"></param>
    /// <returns></returns>
    public bool ValidatePackageFormat(string packagePath)
    {
        if (!Directory.Exists(packagePath))
            return false;

        // 检查是否有 package.json
        var packageJsonPath = Path.Combine(packagePath, "packages.json");
        if (!File.Exists(packageJsonPath))
            return false;

        // 检查是否有主文件
        var mainFiles = SupportedFileExtensions
            .SelectMany(ext => Directory.GetFiles(packagePath, $"*{ext}"))
            .ToList();

        return mainFiles.Count > 0;
    }

    /// <summary>
    /// 提取包元数据
    /// </summary>
    /// <param name="packagePath"></param>
    /// <returns></returns>
    public async Task<PackageMetadata?> ExtractMetadataAsync(string packagePath)
    {
        var packageJsonPath = Path.Combine(packagePath, "packages.json");
        if (!File.Exists(packageJsonPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(packageJsonPath);
            var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            var metadata = new PackageMetadata
            {
                Id = root.GetProperty("id").GetString() ?? "",
                Version = root.GetProperty("version").GetString() ?? "",
                Description = root.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? ""
                    : "",
                Author = root.TryGetProperty("author", out var author)
                    ? author.GetString() ?? ""
                    : ""
            };

            // 解析依赖
            if (!root.TryGetProperty("dependencies", out var depsElement) ||
                depsElement.ValueKind != JsonValueKind.Array) return metadata;
            foreach (var dep in depsElement.EnumerateArray())
            {
                var depInfo = new DependencyInfo
                {
                    PackageId = dep.GetProperty("id").GetString() ?? "",
                    VersionConstraint = dep.GetProperty("version").GetString() ?? "",
                    IsOptional = dep.TryGetProperty("optional", out var opt) && opt.GetBoolean()
                };
                metadata.Dependencies.Add(depInfo);
            }

            return metadata;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析包元数据失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 包安装后的操作
    /// </summary>
    /// <param name="packagePath"></param>
    /// <returns></returns>
    public Task OnPackageInstalledAsync(string packagePath)
    {
        // Old8Lang 包安装后的操作（如果需要）
        Console.WriteLine($"[Old8Lang] 包已安装: {packagePath}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 包卸载前的操作
    /// </summary>
    /// <param name="packagePath"></param>
    /// <returns></returns>
    public Task OnPackageUninstallingAsync(string packagePath)
    {
        // Old8Lang 包卸载前的操作（如果需要）
        Console.WriteLine($"[Old8Lang] 正在卸载包: {packagePath}");
        return Task.CompletedTask;
    }
}