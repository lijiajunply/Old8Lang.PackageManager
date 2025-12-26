using Old8Lang.PackageManager.Core.Interfaces;

namespace Old8Lang.PackageManager.Commands;

/// <summary>
/// 打包命令 - 将包文件夹打包成 .o8pkg 文件
/// </summary>
public class PackCommand(IPackageArchiveService archiveService) : ICommand
{
    public string Name => "pack";
    public string Description => "Pack a package folder into .o8pkg archive";

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return new CommandResult
            {
                Success = false,
                Message = @"Usage: o8pm pack <source-folder> [--output <output-path>]

Arguments:
  <source-folder>    Path to the package folder containing package.json

Options:
  --output <path>    Output path for the .o8pkg file (optional)

Example:
  o8pm pack ./MyPackage
  o8pm pack ./MyPackage --output ./dist/MyPackage.1.0.0.o8pkg",
                ExitCode = 1
            };
        }

        var sourcePath = args[1];
        string? outputPath = null;

        // 解析可选参数
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
        }

        try
        {
            // 验证包结构
            Console.WriteLine($"Validating package structure in: {sourcePath}");
            var (isValid, validationMessage) = await archiveService.ValidatePackageStructureAsync(sourcePath);

            if (!isValid)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = $"✗ Validation failed: {validationMessage}",
                    ExitCode = 1
                };
            }

            Console.WriteLine("✓ Package structure is valid");

            // 读取包元数据
            var package = await archiveService.ReadPackageMetadataAsync(sourcePath);
            if (package != null)
            {
                Console.WriteLine($"  Package: {package.Id} v{package.Version}");
                Console.WriteLine($"  Author: {package.Author}");
                Console.WriteLine($"  Description: {package.Description}");
            }

            // 打包
            Console.WriteLine("\nPacking...");
            var resultPath = await archiveService.PackAsync(sourcePath, outputPath);

            // 获取文件大小
            var fileInfo = new FileInfo(resultPath);
            var sizeInKb = fileInfo.Length / 1024.0;
            var sizeDisplay = sizeInKb > 1024
                ? $"{sizeInKb / 1024.0:F2} MB"
                : $"{sizeInKb:F2} KB";

            return new CommandResult
            {
                Success = true,
                Message = $@"✓ Package created successfully!

Output: {resultPath}
Size: {sizeDisplay}

You can now distribute or publish this package.",
                ExitCode = 0
            };
        }
        catch (DirectoryNotFoundException ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"✗ Directory not found: {ex.Message}",
                ExitCode = 1
            };
        }
        catch (InvalidOperationException ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"✗ Invalid operation: {ex.Message}",
                ExitCode = 1
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"✗ Error packing package: {ex.Message}",
                ExitCode = 1
            };
        }
    }
}
