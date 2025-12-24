using Old8Lang.PackageManager.Core.Commands;
using Old8Lang.PackageManager.Core.Services;

// 创建核心服务
var sourceManager = new PackageSourceManager();
var resolver = new DefaultPackageResolver();
var configManager = new DefaultPackageConfigurationManager();
var installer = new DefaultPackageInstaller(sourceManager, resolver);
var restorer = new PackageRestorer(configManager, installer, sourceManager);

// 创建命令实例
var commands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase)
{
    ["add"] = new AddPackageCommand(installer, sourceManager, configManager),
    ["remove"] = new RemovePackageCommand(installer, configManager),
    ["restore"] = new RestoreCommand(restorer),
    ["search"] = new SearchCommand(sourceManager)
};

// 显示帮助信息
void ShowHelp()
{
    Console.WriteLine("Old8Lang Package Manager");
    Console.WriteLine("Usage: o8pm [command] [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  add <package-id> [version]     Add a package to the project");
    Console.WriteLine("  remove <package-id>            Remove a package from the project");
    Console.WriteLine("  restore                        Restore all packages");
    Console.WriteLine("  search <term>                  Search for packages");
    Console.WriteLine("  help                           Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  o8pm add MyPackage 1.0.0");
    Console.WriteLine("  o8pm remove MyPackage");
    Console.WriteLine("  o8pm restore");
    Console.WriteLine("  o8pm search logger");
}

// 处理命令
if (args.Length == 0 || args[0] == "help")
{
    ShowHelp();
    return 0;
}

var commandName = args[0];

if (!commands.TryGetValue(commandName, out var command))
{
    Console.WriteLine($"Unknown command: {commandName}");
    ShowHelp();
    return 1;
}

try
{
    var result = await command.ExecuteAsync(args);
    Console.WriteLine(result.Message);
    return result.ExitCode;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}