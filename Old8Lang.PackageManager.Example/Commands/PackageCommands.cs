namespace Old8Lang.PackageManager.Commands;

/// <summary>
/// 命令结果
/// </summary>
public class CommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ExitCode { get; set; }
}

/// <summary>
/// 命令接口
/// </summary>
public interface ICommand
{
    /// <summary>
    /// 命令名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 命令描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 执行命令
    /// </summary>
    Task<CommandResult> ExecuteAsync(string[] args);
}

/// <summary>
/// 添加包命令
/// </summary>
public class AddPackageCommand(
    Core.Services.DefaultPackageInstaller installer,
    Core.Services.PackageSourceManager sourceManager,
    Core.Services.DefaultPackageConfigurationManager configManager)
    : ICommand
{
    private readonly Core.Services.PackageSourceManager _sourceManager = sourceManager;

    public string Name => "add";
    public string Description => "Add a package to the project";

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return new CommandResult
            {
                Success = false,
                Message = "Usage: o8pm add <package-id> [version]",
                ExitCode = 1
            };
        }

        var packageId = args[1];
        var version = args.Length > 2 ? args[2] : "latest";

        try
        {
            // 查找配置文件
            var configPath = FindConfigFile();

            // 添加到配置文件
            var addedToConfig = await configManager.AddPackageReferenceAsync(configPath, packageId, version);
            if (!addedToConfig)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = $"Failed to add {packageId} to configuration",
                    ExitCode = 1
                };
            }

            // 安装包
            var config = await configManager.ReadConfigurationAsync(configPath);
            var installResult = await installer.InstallPackageAsync(packageId, version, config.InstallPath);

            return new CommandResult
            {
                Success = installResult.Success,
                Message = installResult.Message,
                ExitCode = installResult.Success ? 0 : 1
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"Error adding package: {ex.Message}",
                ExitCode = 1
            };
        }
    }

    private string FindConfigFile()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(currentDir, "o8packages.json");

        if (File.Exists(configPath))
            return configPath;

        // 如果配置文件不存在，使用默认路径
        return configPath;
    }
}

/// <summary>
/// 移除包命令
/// </summary>
public class RemovePackageCommand(
    Core.Services.DefaultPackageInstaller installer,
    Core.Services.DefaultPackageConfigurationManager configManager)
    : ICommand
{
    public string Name => "remove";
    public string Description => "Remove a package from the project";

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return new CommandResult
            {
                Success = false,
                Message = "Usage: o8pm remove <package-id>",
                ExitCode = 1
            };
        }

        var packageId = args[1];

        try
        {
            var configPath = FindConfigFile();
            var config = await configManager.ReadConfigurationAsync(configPath);

            // 从配置文件移除
            var removedFromConfig = await configManager.RemovePackageReferenceAsync(configPath, packageId);

            // 从磁盘移除所有版本
            var installedPackages = await installer.GetInstalledPackagesAsync(config.InstallPath);
            var packagesToRemove = installedPackages.Where(p => p.Id == packageId);

            foreach (var package in packagesToRemove)
            {
                await installer.UninstallPackageAsync(package.Id, package.Version, config.InstallPath);
            }

            return new CommandResult
            {
                Success = true,
                Message = $"Package {packageId} removed successfully",
                ExitCode = 0
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"Error removing package: {ex.Message}",
                ExitCode = 1
            };
        }
    }

    private string FindConfigFile()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(currentDir, "o8packages.json");

        if (File.Exists(configPath))
            return configPath;

        return configPath;
    }
}

/// <summary>
/// 还原包命令
/// </summary>
public class RestoreCommand : ICommand
{
    private readonly Core.Services.PackageRestorer _restorer;

    public string Name => "restore";
    public string Description => "Restore all packages defined in the configuration file";

    public RestoreCommand(Core.Services.PackageRestorer restorer)
    {
        _restorer = restorer;
    }

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(currentDir, "o8packages.json");

            if (!File.Exists(configPath))
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "No o8packages.json file found in current directory",
                    ExitCode = 1
                };
            }

            var result = await _restorer.RestorePackagesAsync(configPath);

            return new CommandResult
            {
                Success = result.Success,
                Message = result.Message,
                ExitCode = result.Success ? 0 : 1
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"Error restoring packages: {ex.Message}",
                ExitCode = 1
            };
        }
    }
}

/// <summary>
/// 搜索包命令
/// </summary>
public class SearchCommand : ICommand
{
    private readonly Core.Services.PackageSourceManager _sourceManager;

    public string Name => "search";
    public string Description => "Search for packages";

    public SearchCommand(Core.Services.PackageSourceManager sourceManager)
    {
        _sourceManager = sourceManager;
    }

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return new CommandResult
            {
                Success = false,
                Message = "Usage: o8pm search <search-term>",
                ExitCode = 1
            };
        }

        var searchTerm = args[1];

        try
        {
            var results = await _sourceManager.SearchPackagesAsync(searchTerm);

            if (!results.Any())
            {
                return new CommandResult
                {
                    Success = true,
                    Message = $"No packages found for '{searchTerm}'",
                    ExitCode = 0
                };
            }

            var output = $"Found {results.Count()} packages:\n";
            foreach (var package in results.Take(20)) // 限制显示20个结果
            {
                output += $"  {package.Id} {package.Version} - {package.Description}\n";
            }

            return new CommandResult
            {
                Success = true,
                Message = output.Trim(),
                ExitCode = 0
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"Error searching packages: {ex.Message}",
                ExitCode = 1
            };
        }
    }
}