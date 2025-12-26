using Old8Lang.PackageManager.Core.Interfaces;

namespace Old8Lang.PackageManager.Commands;

/// <summary>
/// 解包命令 - 解压 .o8pkg 文件到文件夹
/// </summary>
public class UnpackCommand(IPackageArchiveService archiveService) : ICommand
{
    public string Name => "unpack";
    public string Description => "Unpack a .o8pkg archive to a folder";

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return new CommandResult
            {
                Success = false,
                Message = @"Usage: o8pm unpack <package-file> [<destination-folder>]

Arguments:
  <package-file>         Path to the .o8pkg file
  <destination-folder>   Destination folder (optional, defaults to package name)

Example:
  o8pm unpack MyPackage.1.0.0.o8pkg
  o8pm unpack MyPackage.1.0.0.o8pkg ./extracted",
                ExitCode = 1
            };
        }

        var packagePath = args[1];
        string? destinationPath = args.Length > 2 ? args[2] : null;

        try
        {
            // 验证包文件存在
            if (!File.Exists(packagePath))
            {
                return new CommandResult
                {
                    Success = false,
                    Message = $"✗ Package file not found: {packagePath}",
                    ExitCode = 1
                };
            }

            // 如果没有指定目标路径，使用包文件名（去掉扩展名）
            if (string.IsNullOrEmpty(destinationPath))
            {
                var fileName = Path.GetFileNameWithoutExtension(packagePath);
                var parentDir = Path.GetDirectoryName(packagePath) ?? Directory.GetCurrentDirectory();
                destinationPath = Path.Combine(parentDir, fileName);
            }

            Console.WriteLine($"Unpacking: {packagePath}");
            Console.WriteLine($"Destination: {destinationPath}");

            // 解包
            await archiveService.UnpackAsync(packagePath, destinationPath);

            // 读取包元数据
            var package = await archiveService.ReadPackageMetadataAsync(destinationPath);

            var message = "✓ Package unpacked successfully!\n\n" +
                          $"Destination: {destinationPath}";

            if (package != null)
            {
                message += $"\n\nPackage Information:\n" +
                          $"  Id: {package.Id}\n" +
                          $"  Version: {package.Version}\n" +
                          $"  Author: {package.Author}\n" +
                          $"  Description: {package.Description}";
            }

            return new CommandResult
            {
                Success = true,
                Message = message,
                ExitCode = 0
            };
        }
        catch (FileNotFoundException ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"✗ File not found: {ex.Message}",
                ExitCode = 1
            };
        }
        catch (ArgumentException ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"✗ Invalid argument: {ex.Message}",
                ExitCode = 1
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"✗ Error unpacking package: {ex.Message}",
                ExitCode = 1
            };
        }
    }
}
