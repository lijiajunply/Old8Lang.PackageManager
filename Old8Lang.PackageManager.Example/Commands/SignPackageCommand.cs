using Old8Lang.PackageManager.Core.Interfaces;

namespace Old8Lang.PackageManager.Commands;

/// <summary>
/// 签名包命令
/// </summary>
public class SignPackageCommand(IPackageSignatureService signatureService) : ICommand
{
    public string Name => "sign";
    public string Description => "Sign a package with a certificate";

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return new CommandResult
            {
                Success = false,
                Message = "Usage: o8pm sign <package-path> [--cert <cert-path>] [--cert-password <password>]",
                ExitCode = 1
            };
        }

        var packagePath = args[1];
        string? certPath = null;
        string? certPassword = null;

        // 解析参数
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--cert" && i + 1 < args.Length)
            {
                certPath = args[++i];
            }
            else if (args[i] == "--cert-password" && i + 1 < args.Length)
            {
                certPassword = args[++i];
            }
        }

        try
        {
            if (!File.Exists(packagePath))
            {
                return new CommandResult
                {
                    Success = false,
                    Message = $"Package file not found: {packagePath}",
                    ExitCode = 1
                };
            }

            // 加载或生成证书
            var certificate = string.IsNullOrEmpty(certPath)
                ? GenerateSelfSignedCertificate()
                : await LoadCertificateAsync(certPath, certPassword);

            // 签名包
            var signature = await signatureService.SignPackageAsync(packagePath, certificate);

            // 保存签名文件
            var signatureFile = packagePath + ".sig";
            await signatureService.WriteSignatureAsync(signature, signatureFile);

            return new CommandResult
            {
                Success = true,
                Message =
                    $"Package signed successfully!\nSignature file: {signatureFile}\nSigned by: {signature.Signer.Name ?? signature.Signer.Email ?? "Unknown"}\nThumbprint: {signature.Signer.CertificateThumbprint}",
                ExitCode = 0
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"Error signing package: {ex.Message}",
                ExitCode = 1
            };
        }
    }

    private System.Security.Cryptography.X509Certificates.X509Certificate2 GenerateSelfSignedCertificate()
    {
        Console.WriteLine("No certificate specified, generating self-signed certificate...");
        var certificate = signatureService.GenerateSelfSignedCertificate("O8PM Package Signer");
        Console.WriteLine($"Generated certificate with thumbprint: {certificate.Thumbprint}");
        return certificate;
    }

    private async Task<System.Security.Cryptography.X509Certificates.X509Certificate2> LoadCertificateAsync(
        string certPath, string? password)
    {
        if (!File.Exists(certPath))
        {
            throw new FileNotFoundException($"Certificate file not found: {certPath}");
        }

        return await signatureService.LoadCertificateAsync(certPath, password);
    }
}