using Old8Lang.PackageManager.Core.Interfaces;

namespace Old8Lang.PackageManager.Commands;

/// <summary>
/// 验证包签名命令
/// </summary>
public class VerifyPackageCommand(IPackageSignatureService signatureService) : ICommand
{
    public string Name => "verify";
    public string Description => "Verify a package signature";

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return new CommandResult
            {
                Success = false,
                Message = "Usage: o8pm verify <package-path>",
                ExitCode = 1
            };
        }

        var packagePath = args[1];

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

            // 查找签名文件
            var signatureFile = packagePath + ".sig";
            if (!File.Exists(signatureFile))
            {
                return new CommandResult
                {
                    Success = false,
                    Message = $"Signature file not found: {signatureFile}",
                    ExitCode = 1
                };
            }

            // 读取并解析签名
            var signature = await signatureService.ReadSignatureAsync(signatureFile);

            if (signature == null)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Invalid signature file format",
                    ExitCode = 1
                };
            }

            // 验证签名
            var isValid = await signatureService.VerifySignatureAsync(packagePath, signature);

            if (isValid)
            {
                return new CommandResult
                {
                    Success = true,
                    Message = $"✓ Signature is valid\nSigned by: {signature.Signer.Name ?? signature.Signer.Email ?? "Unknown"}\nSigned at: {signature.Timestamp:yyyy-MM-dd HH:mm:ss}\nCertificate: {signature.Signer.CertificateThumbprint}",
                    ExitCode = 0
                };
            }
            else
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "✗ Signature verification failed! Package may have been tampered with.",
                    ExitCode = 1
                };
            }
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Success = false,
                Message = $"Error verifying signature: {ex.Message}",
                ExitCode = 1
            };
        }
    }
}
