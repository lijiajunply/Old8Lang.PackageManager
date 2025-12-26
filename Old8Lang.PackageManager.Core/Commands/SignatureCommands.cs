using Old8Lang.PackageManager.Core.Models;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Old8Lang.PackageManager.Core.Commands;

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
/// 签名包命令
/// </summary>
public class SignPackageCommand : ICommand
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

            X509Certificate2 certificate;

            // 加载或生成证书
            if (string.IsNullOrEmpty(certPath))
            {
                // 生成自签名证书
                Console.WriteLine("No certificate specified, generating self-signed certificate...");
                certificate = GenerateSelfSignedCertificate("O8PM Package Signer");
                Console.WriteLine($"Generated certificate with thumbprint: {certificate.Thumbprint}");
            }
            else
            {
                if (!File.Exists(certPath))
                {
                    return new CommandResult
                    {
                        Success = false,
                        Message = $"Certificate file not found: {certPath}",
                        ExitCode = 1
                    };
                }

                var certBytes = await File.ReadAllBytesAsync(certPath);
                certificate = string.IsNullOrEmpty(certPassword)
                    ? new X509Certificate2(certBytes)
                    : new X509Certificate2(certBytes, certPassword);
            }

            if (!certificate.HasPrivateKey)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Certificate does not contain a private key",
                    ExitCode = 1
                };
            }

            // 签名包
            var signature = await SignPackageAsync(packagePath, certificate);

            // 保存签名文件
            var signatureFile = packagePath + ".sig";
            var signatureJson = System.Text.Json.JsonSerializer.Serialize(signature, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(signatureFile, signatureJson);

            return new CommandResult
            {
                Success = true,
                Message = $"Package signed successfully!\nSignature file: {signatureFile}\nSigned by: {signature.Signer.Name ?? signature.Signer.Email ?? "Unknown"}\nThumbprint: {signature.Signer.CertificateThumbprint}",
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

    private async Task<PackageSignature> SignPackageAsync(string packagePath, X509Certificate2 certificate)
    {
        const string hashAlgorithm = "SHA256";

        // 计算包哈希
        await using var stream = File.OpenRead(packagePath);
        var packageHash = await SHA256.HashDataAsync(stream);

        // 使用 RSA 私钥签名
        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa == null)
        {
            throw new InvalidOperationException("Unable to get RSA private key");
        }

        var signatureBytes = rsa.SignData(packageHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // 构建签名信息
        return new PackageSignature
        {
            Algorithm = $"RSA-{hashAlgorithm}",
            SignatureData = Convert.ToBase64String(signatureBytes),
            Timestamp = DateTimeOffset.UtcNow,
            PackageHash = Convert.ToBase64String(packageHash),
            HashAlgorithm = hashAlgorithm,
            Signer = new SignerInfo
            {
                CertificateThumbprint = certificate.Thumbprint,
                PublicKey = certificate.ExportCertificatePem(),
                Name = certificate.GetNameInfo(X509NameType.SimpleName, false),
                Email = certificate.GetNameInfo(X509NameType.EmailName, false),
                NotBefore = certificate.NotBefore,
                NotAfter = certificate.NotAfter
            }
        };
    }

    private X509Certificate2 GenerateSelfSignedCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            $"CN={subjectName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                true));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));

        return certificate;
    }
}

/// <summary>
/// 验证包签名命令
/// </summary>
public class VerifyPackageCommand : ICommand
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
            var signatureJson = await File.ReadAllTextAsync(signatureFile);
            var signature = System.Text.Json.JsonSerializer.Deserialize<PackageSignature>(signatureJson);

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
            var isValid = await VerifySignatureAsync(packagePath, signature);

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

    private async Task<bool> VerifySignatureAsync(string packagePath, PackageSignature signature)
    {
        try
        {
            // 计算包哈希
            await using var stream = File.OpenRead(packagePath);
            var packageHash = await SHA256.HashDataAsync(stream);
            var packageHashBase64 = Convert.ToBase64String(packageHash);

            // 验证哈希值
            if (signature.PackageHash != packageHashBase64)
            {
                Console.WriteLine("Package hash mismatch!");
                return false;
            }

            // 从签名中提取公钥并验证
            var publicKeyPem = signature.Signer.PublicKey;
            using var cert = X509Certificate2.CreateFromPem(publicKeyPem);
            using var rsa = cert.GetRSAPublicKey();

            if (rsa == null)
            {
                Console.WriteLine("Unable to get RSA public key from certificate");
                return false;
            }

            var signatureBytes = Convert.FromBase64String(signature.SignatureData);
            var isValid = rsa.VerifyData(packageHash, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return isValid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Verification error: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// 证书管理命令
/// </summary>
public class CertificateCommand : ICommand
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
            using var rsa = RSA.Create(2048);

            var subjectName = string.IsNullOrEmpty(email) ? $"CN={name}" : $"CN={name}, E={email}";
            var request = new CertificateRequest(
                subjectName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    true));

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(years));

            // 保存证书
            outputPath ??= $"{name.Replace(" ", "_")}.pfx";
            var pfxBytes = certificate.Export(X509ContentType.Pfx, "");
            await File.WriteAllBytesAsync(outputPath, pfxBytes);

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
            if (!File.Exists(certPath))
            {
                return new CommandResult
                {
                    Success = false,
                    Message = $"Certificate file not found: {certPath}",
                    ExitCode = 1
                };
            }

            var certBytes = await File.ReadAllBytesAsync(certPath);
            using var cert = new X509Certificate2(certBytes);

            var info = $@"Certificate Information:
Subject: {cert.Subject}
Issuer: {cert.Issuer}
Thumbprint: {cert.Thumbprint}
Valid From: {cert.NotBefore:yyyy-MM-dd HH:mm:ss}
Valid Until: {cert.NotAfter:yyyy-MM-dd HH:mm:ss}
Has Private Key: {cert.HasPrivateKey}
Status: {(cert.NotAfter < DateTime.UtcNow ? "EXPIRED" : "Valid")}";

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
            if (!File.Exists(certPath))
            {
                return new CommandResult
                {
                    Success = false,
                    Message = $"Certificate file not found: {certPath}",
                    ExitCode = 1
                };
            }

            var certBytes = await File.ReadAllBytesAsync(certPath);
            var cert = new X509Certificate2(certBytes);

            var exportBytes = string.IsNullOrEmpty(password)
                ? cert.Export(X509ContentType.Cert)
                : cert.Export(X509ContentType.Pfx, password);

            await File.WriteAllBytesAsync(outputPath, exportBytes);

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
