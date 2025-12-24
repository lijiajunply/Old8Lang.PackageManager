using Old8Lang.PackageManager.Core.Interfaces;
using System.Text.Json;

namespace Old8Lang.PackageManager.Core.Adapters;

/// <summary>
/// Old8Lang 语言适配器示例
/// </summary>
public class Old8LangAdapter : ILanguageAdapter
{
    public string LanguageName => "old8lang";

    public IEnumerable<string> SupportedFileExtensions => new[] { ".o8", ".old8", ".ol" };

    public string ConfigurationFileName => "o8packages.json";

    public bool ValidatePackageFormat(string packagePath)
    {
        if (!Directory.Exists(packagePath))
            return false;

        // 检查是否有 package.json
        var packageJsonPath = Path.Combine(packagePath, "package.json");
        if (!File.Exists(packageJsonPath))
            return false;

        // 检查是否有主文件
        var mainFiles = SupportedFileExtensions
            .SelectMany(ext => Directory.GetFiles(packagePath, $"*{ext}"))
            .ToList();

        return mainFiles.Count > 0;
    }

    public async Task<PackageMetadata?> ExtractMetadataAsync(string packagePath)
    {
        var packageJsonPath = Path.Combine(packagePath, "package.json");
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
            if (root.TryGetProperty("dependencies", out var depsElement) &&
                depsElement.ValueKind == JsonValueKind.Array)
            {
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
            }

            return metadata;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析包元数据失败: {ex.Message}");
            return null;
        }
    }

    public Task OnPackageInstalledAsync(string packagePath)
    {
        // Old8Lang 包安装后的操作（如果需要）
        Console.WriteLine($"[Old8Lang] 包已安装: {packagePath}");
        return Task.CompletedTask;
    }

    public Task OnPackageUninstallingAsync(string packagePath)
    {
        // Old8Lang 包卸载前的操作（如果需要）
        Console.WriteLine($"[Old8Lang] 正在卸载包: {packagePath}");
        return Task.CompletedTask;
    }
}
