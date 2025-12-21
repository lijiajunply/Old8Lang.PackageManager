using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using System.Text.Json;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 默认包配置管理器
/// </summary>
public class DefaultPackageConfigurationManager : IPackageConfigurationManager
{
    public async Task<PackageConfiguration> ReadConfigurationAsync(string configPath)
    {
        if (!File.Exists(configPath))
        {
            // 返回默认配置
            return new PackageConfiguration
            {
                ProjectName = Path.GetFileName(Path.GetDirectoryName(configPath) ?? ""),
                Sources = GetDefaultSources()
            };
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var configuration = JsonSerializer.Deserialize<PackageConfiguration>(json);

            return configuration ?? new PackageConfiguration
            {
                Sources = GetDefaultSources()
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read package configuration: {ex.Message}", ex);
        }
    }

    public async Task<bool> WriteConfigurationAsync(string configPath, PackageConfiguration configuration)
    {
        try
        {
            var directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(configPath, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write package configuration: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AddPackageReferenceAsync(string configPath, string packageId, string version)
    {
        try
        {
            var configuration = await ReadConfigurationAsync(configPath);

            // 检查是否已存在
            var existingRef = configuration.References.FirstOrDefault(r =>
                r.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));

            if (existingRef != null)
            {
                existingRef.Version = version;
            }
            else
            {
                configuration.References.Add(new PackageReference
                {
                    PackageId = packageId,
                    Version = version
                });
            }

            return await WriteConfigurationAsync(configPath, configuration);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add package reference: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RemovePackageReferenceAsync(string configPath, string packageId)
    {
        try
        {
            var configuration = await ReadConfigurationAsync(configPath);

            var reference = configuration.References.FirstOrDefault(r =>
                r.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));

            if (reference != null)
            {
                configuration.References.Remove(reference);
                return await WriteConfigurationAsync(configPath, configuration);
            }

            return true; // 不存在也算成功
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to remove package reference: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<PackageReference>> GetPackageReferencesAsync(string configPath)
    {
        try
        {
            var configuration = await ReadConfigurationAsync(configPath);
            return configuration.References;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get package references: {ex.Message}");
            return Enumerable.Empty<PackageReference>();
        }
    }

    private List<PackageSource> GetDefaultSources()
    {
        return
        [
            new PackageSource()
            {
                Name = "Old8Lang Official",
                Source = "https://packages.old8lang.org/v3/index.json",
                IsEnabled = true
            },

            new PackageSource()
            {
                Name = "Local Packages",
                Source = "./local-packages",
                IsEnabled = true
            }
        ];
    }
}