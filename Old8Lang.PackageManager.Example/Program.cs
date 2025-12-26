using Old8Lang.PackageManager.Commands;
using Old8Lang.PackageManager.Core.Services;
using AddPackageCommand = Old8Lang.PackageManager.Commands.AddPackageCommand;
using ICommand = Old8Lang.PackageManager.Commands.ICommand;
using RemovePackageCommand = Old8Lang.PackageManager.Commands.RemovePackageCommand;
using RestoreCommand = Old8Lang.PackageManager.Commands.RestoreCommand;
using SearchCommand = Old8Lang.PackageManager.Commands.SearchCommand;

// 创建核心服务
var sourceManager = new PackageSourceManager();
var resolver = new DefaultPackageResolver();
var configManager = new DefaultPackageConfigurationManager();
var installer = new DefaultPackageInstaller(sourceManager, resolver);
var restorer = new PackageRestorer(configManager, installer, sourceManager);
var signatureService = new PackageSignatureService();

// 创建命令包装器
var coreCommands = new List<ICommand>
{
    new AddPackageCommand(installer, sourceManager, configManager),
    new RemovePackageCommand(installer, configManager),
    new RestoreCommand(restorer),
    new SearchCommand(sourceManager)
};

var exampleCommands = new List<ICommand>
{
    new SignPackageCommand(signatureService),
    new VerifyPackageCommand(signatureService),
    new CertificateCommand(signatureService)
};

//创建一个统一的命令映射
var commands =
    new Dictionary<string, Func<string[], Task<(bool Success, string Message, int ExitCode)>>>(StringComparer
        .OrdinalIgnoreCase);

// 添加核心命令
foreach (var cmd in coreCommands)
{
    commands[cmd.Name] = async (args) =>
    {
        var result = await cmd.ExecuteAsync(args);
        return (result.Success, result.Message, result.ExitCode);
    };
}

// 添加示例命令
foreach (var cmd in exampleCommands)
{
    commands[cmd.Name] = async (args) =>
    {
        var result = await cmd.ExecuteAsync(args);
        return (result.Success, result.Message, result.ExitCode);
    };
}

// 显示帮助信息
void ShowHelp()
{
    Console.WriteLine("Old8Lang Package Manager");
    Console.WriteLine("Usage: o8pm [command] [options]");
    Console.WriteLine();
    Console.WriteLine("Package Management Commands:");
    Console.WriteLine("  add <package-id> [version]     Add a package to the project");
    Console.WriteLine("  remove <package-id>            Remove a package from the project");
    Console.WriteLine("  restore                        Restore all packages");
    Console.WriteLine("  search <term>                  Search for packages");
    Console.WriteLine();
    Console.WriteLine("Signature Commands:");
    Console.WriteLine("  sign <package-path>            Sign a package with a certificate");
    Console.WriteLine("  verify <package-path>          Verify a package signature");
    Console.WriteLine("  cert <subcommand>              Manage certificates");
    Console.WriteLine();
    Console.WriteLine("Other:");
    Console.WriteLine("  help                           Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  o8pm add MyPackage 1.0.0");
    Console.WriteLine("  o8pm remove MyPackage");
    Console.WriteLine("  o8pm restore");
    Console.WriteLine("  o8pm search logger");
    Console.WriteLine("  o8pm sign MyPackage.1.0.0.o8pkg");
    Console.WriteLine("  o8pm verify MyPackage.1.0.0.o8pkg");
    Console.WriteLine("  o8pm cert generate \"My Certificate\"");
}

// 处理命令
if (args.Length == 0 || args[0] == "help")
{
    ShowHelp();
    return 0;
}

var commandName = args[0];

if (!commands.TryGetValue(commandName, out var commandFunc))
{
    Console.WriteLine($"Unknown command: {commandName}");
    ShowHelp();
    return 1;
}

try
{
    var (success, message, exitCode) = await commandFunc(args);
    Console.WriteLine(message);
    return exitCode;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}