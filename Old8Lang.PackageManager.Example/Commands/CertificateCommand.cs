using Old8Lang.PackageManager.Core.Interfaces;

namespace Old8Lang.PackageManager.Commands;

/// <summary>
/// 证书管理命令
/// </summary>
public class CertificateCommand(IPackageSignatureService signatureService) : ICommand
{
    public string Name => "cert";
    public string Description => "Manage certificates (generate, list, trust)";

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return new CommandResult
            {
                Success = false,
                Message = @"Usage: o8pm cert <subcommand> [options]

Subcommands:
  generate <name> [--email <email>] [--years <years>] [--output <path>]
    Generate a new self-signed certificate

  info <cert-path>
    Display certificate information

  export <cert-path> <output-path> [--password <password>]
    Export certificate to file",
                ExitCode = 1
            };
        }

        var subcommand = args[1].ToLowerInvariant();

        return subcommand switch
        {
            "generate" => await GenerateCertificateAsync(args),
            "info" => await ShowCertificateInfoAsync(args),
            "export" => await ExportCertificateAsync(args),
            _ => new CommandResult
            {
                Success = false,
                Message = $"Unknown subcommand: {subcommand}",
                ExitCode = 1
            }
        };
    }

    private async Task<CommandResult> GenerateCertificateAsync(string[] args)
    {
        if (args.Length < 3)
        {
            return new CommandResult
            {
                Success = false,
                Message = "Usage: o8pm cert generate <name> [--email <email>] [--years <years>] [--output <path>]",
                ExitCode = 1
            };
        }

        var name = args[2];
        string? email = null;
        int years = 5;
        string? outputPath = null;

        for (int i = 3; i < args.Length; i++)
        {
            if (args[i] == "--email" && i + 1 < args.Length)
            {
                email = args[++i];
            }
            else if (args[i] == "--years" && i + 1 < args.Length)
            {
                years = int.Parse(args[++i]);
            }
            else if (args[i] == "--output" && i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
        }

        try
        {
            var certificate = signatureService.GenerateSelfSignedCertificate(name, email, years);

            // 保存证书
            outputPath ??= $"{name.Replace(" ", "_")}.pfx";
            await signatureService.ExportCertificateAsync(certificate, outputPath, "");

            return new CommandResult
            {
                Success = true,
                Message = $"Certificate generated successfully!\nOutput: {outputPath}\nThumbprint: {certificate.Thumbprint}\nValid until: {certificate.NotAfter:yyyy-MM-dd}",
                ExitCode = 0
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"Error generating certificate: {ex.Message}",
                ExitCode = 1
            };
        }
    }

    private async Task<CommandResult> ShowCertificateInfoAsync(string[] args)
    {
        if (args.Length < 3)
        {
            return new CommandResult
            {
                Success = false,
                Message = "Usage: o8pm cert info <cert-path>",
                ExitCode = 1
            };
        }

        var certPath = args[2];

        try
        {
            var cert = await signatureService.LoadCertificateAsync(certPath);
            var info = signatureService.GetCertificateInfo(cert);

            return new CommandResult
            {
                Success = true,
                Message = info,
                ExitCode = 0
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"Error reading certificate: {ex.Message}",
                ExitCode = 1
            };
        }
    }

    private async Task<CommandResult> ExportCertificateAsync(string[] args)
    {
        if (args.Length < 4)
        {
            return new CommandResult
            {
                Success = false,
                Message = "Usage: o8pm cert export <cert-path> <output-path> [--password <password>]",
                ExitCode = 1
            };
        }

        var certPath = args[2];
        var outputPath = args[3];
        string? password = null;

        for (int i = 4; i < args.Length; i++)
        {
            if (args[i] == "--password" && i + 1 < args.Length)
            {
                password = args[++i];
            }
        }

        try
        {
            var cert = await signatureService.LoadCertificateAsync(certPath);
            await signatureService.ExportCertificateAsync(cert, outputPath, password);

            return new CommandResult
            {
                Success = true,
                Message = $"Certificate exported to: {outputPath}",
                ExitCode = 0
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"Error exporting certificate: {ex.Message}",
                ExitCode = 1
            };
        }
    }
}
