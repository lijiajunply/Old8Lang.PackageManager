using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 包打包服务实现
/// </summary>
public class PackageArchiveService : IPackageArchiveService
{
    /// <summary>
    /// 包元数据文件名
    /// </summary>
    public string PackageMetadataFileName { get; set; } = "package.json";

    /// <summary>
    /// 包扩展名
    /// </summary>
    public string PackageExtension { get; set; } = ".o8pkg";

    /// <summary>
    /// 将包文件夹打包成 .o8pkg 压缩包
    /// </summary>
    public async Task<string> PackAsync(string sourcePath, string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
        }

        // 验证包结构
        var (isValid, message) = await ValidatePackageStructureAsync(sourcePath);
        if (!isValid)
        {
            throw new InvalidOperationException($"Invalid package structure: {message}");
        }

        // 读取包元数据
        var package = await ReadPackageMetadataAsync(sourcePath);
        if (package == null)
        {
            throw new InvalidOperationException($"Could not read package metadata from {sourcePath}");
        }

        // 确定输出路径
        if (string.IsNullOrEmpty(outputPath))
        {
            var parentDir = Path.GetDirectoryName(sourcePath) ?? Directory.GetCurrentDirectory();
            outputPath = Path.Combine(parentDir, $"{package.Id}.{package.Version}{PackageExtension}");
        }
        else if (!outputPath.EndsWith(PackageExtension, StringComparison.OrdinalIgnoreCase))
        {
            outputPath += PackageExtension;
        }

        // 如果输出文件已存在，删除它
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        // 创建 ZIP 压缩包
        await Task.Run(() => { ZipFile.CreateFromDirectory(sourcePath, outputPath, CompressionLevel.Optimal, false); },
            cancellationToken);

        // 计算包文件大小和校验和
        var fileInfo = new FileInfo(outputPath);
        package.Size = fileInfo.Length;
        package.FilePath = outputPath;
        package.Checksum = await ComputeChecksumAsync(outputPath, cancellationToken);
        package.PublishedAt = DateTime.UtcNow;

        return outputPath;
    }

    /// <summary>
    /// 解包 .o8pkg 文件到指定文件夹
    /// </summary>
    public async Task UnpackAsync(string packagePath, string destinationPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException($"Package file not found: {packagePath}", packagePath);
        }

        if (!packagePath.EndsWith(PackageExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid package file extension. Expected {PackageExtension}",
                nameof(packagePath));
        }

        // 如果目标文件夹已存在，清空它
        if (Directory.Exists(destinationPath))
        {
            Directory.Delete(destinationPath, true);
        }

        Directory.CreateDirectory(destinationPath);

        // 解压 ZIP 文件
        await Task.Run(() => { ZipFile.ExtractToDirectory(packagePath, destinationPath); }, cancellationToken);
    }

    /// <summary>
    /// 验证包文件夹结构是否有效
    /// </summary>
    public async Task<(bool IsValid, string Message)> ValidatePackageStructureAsync(string sourcePath)
    {
        if (!Directory.Exists(sourcePath))
        {
            return (false, "Source directory does not exist");
        }

        // 检查 package.json 是否存在
        var metadataPath = Path.Combine(sourcePath, PackageMetadataFileName);
        if (!File.Exists(metadataPath))
        {
            return (false, $"Missing required file: {PackageMetadataFileName}");
        }

        // 尝试读取和解析 package.json
        try
        {
            var json = await File.ReadAllTextAsync(metadataPath);
            var package = JsonSerializer.Deserialize<Package>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (package == null)
            {
                return (false, $"Could not parse {PackageMetadataFileName}");
            }

            // 验证必需字段
            if (string.IsNullOrWhiteSpace(package.Id))
            {
                return (false, "Package Id is required in package.json");
            }

            if (string.IsNullOrWhiteSpace(package.Version))
            {
                return (false, "Package Version is required in package.json");
            }

            // 检查是否有 lib 文件夹（可选检查）
            var libPath = Path.Combine(sourcePath, "lib");
            if (!Directory.Exists(libPath))
            {
                // 警告但不失败
                // 某些包可能只包含元数据或文档
            }

            return (true, "Package structure is valid");
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid JSON in {PackageMetadataFileName}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Error validating package structure: {ex.Message}");
        }
    }

    /// <summary>
    /// 从包文件夹中读取包元数据
    /// </summary>
    public async Task<Package?> ReadPackageMetadataAsync(string sourcePath)
    {
        var metadataPath = Path.Combine(sourcePath, PackageMetadataFileName);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(metadataPath);
            return JsonSerializer.Deserialize<Package>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 计算文件的 SHA256 校验和
    /// </summary>
    private async Task<string> ComputeChecksumAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}