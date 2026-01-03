using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Exceptions;
using Old8Lang.PackageManager.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 包签名服务实现
/// </summary>
public class PackageSignatureService : IPackageSignatureService
{
    private const string DefaultHashAlgorithm = "SHA256";
    private const int DefaultRsaKeySize = 2048;
    private readonly ILogger<PackageSignatureService> _logger;

    /// <summary>
    /// 包签名服务
    /// </summary>
    /// <param name="logger">日志记录器（可选）</param>
    public PackageSignatureService(ILogger<PackageSignatureService>? logger = null)
    {
        _logger = logger ?? NullLogger<PackageSignatureService>.Instance;
    }

    /// <summary>
    /// 签名包
    /// </summary>
    /// <param name="packagePath">包路径</param>
    /// <param name="certificate">证书</param>
    /// <returns>包签名</returns>
    /// <exception cref="PackageNotFoundException">文件未找到</exception>
    /// <exception cref="PackageSignatureException">签名失败</exception>
    public async Task<PackageSignature> SignPackageAsync(string packagePath, X509Certificate2 certificate)
    {
        _logger.LogInformation("Signing package at path '{PackagePath}'", packagePath);

        if (!File.Exists(packagePath))
        {
            var ex = new PackageNotFoundException(Path.GetFileNameWithoutExtension(packagePath), null,
                new FileNotFoundException($"Package file not found: {packagePath}", packagePath));
            _logger.LogError(ex, "Package file not found at '{PackagePath}'", packagePath);
            throw ex;
        }

        if (!certificate.HasPrivateKey)
        {
            var ex = new PackageSignatureException(
                "Certificate does not contain a private key",
                Path.GetFileNameWithoutExtension(packagePath));
            _logger.LogError(ex, "Certificate does not contain private key");
            throw ex;
        }

        try
        {
            // 计算包哈希
            await using var stream = File.OpenRead(packagePath);
            var packageHash = await SHA256.HashDataAsync(stream);

            // 使用 RSA 私钥签名
            using var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new PackageSignatureException(
                    "Unable to get RSA private key from certificate",
                    Path.GetFileNameWithoutExtension(packagePath));
            }

            var signatureBytes = rsa.SignData(packageHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // 构建签名信息
            var signature = new PackageSignature
            {
                Algorithm = $"RSA-{DefaultHashAlgorithm}",
                SignatureData = Convert.ToBase64String(signatureBytes),
                Timestamp = DateTimeOffset.UtcNow,
                PackageHash = Convert.ToBase64String(packageHash),
                HashAlgorithm = DefaultHashAlgorithm,
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

            _logger.LogInformation("Successfully signed package '{PackagePath}' with certificate '{Thumbprint}'",
                packagePath, certificate.Thumbprint);
            return signature;
        }
        catch (PackageSignatureException)
        {
            throw;
        }
        catch (CryptographicException ex)
        {
            var signEx = new PackageSignatureException(
                $"Cryptographic error while signing package: {ex.Message}",
                ex, Path.GetFileNameWithoutExtension(packagePath));
            _logger.LogError(signEx, "Cryptographic error while signing package '{PackagePath}'", packagePath);
            throw signEx;
        }
        catch (Exception ex)
        {
            var signEx = new PackageSignatureException(
                $"Failed to sign package: {ex.Message}",
                ex, Path.GetFileNameWithoutExtension(packagePath));
            _logger.LogError(signEx, "Unexpected error while signing package '{PackagePath}'", packagePath);
            throw signEx;
        }
    }

    /// <summary>
    /// 验证签名
    /// </summary>
    /// <param name="packagePath">包路径</param>
    /// <param name="signature">签名信息</param>
    /// <returns>是否验证通过</returns>
    /// <exception cref="PackageNotFoundException">包文件未找到</exception>
    public async Task<bool> VerifySignatureAsync(string packagePath, PackageSignature signature)
    {
        var packageId = Path.GetFileNameWithoutExtension(packagePath);
        _logger.VerifyingSignature(packageId, signature.Version ?? "unknown");

        if (!File.Exists(packagePath))
        {
            var ex = new PackageNotFoundException(packageId, null,
                new FileNotFoundException($"Package file not found: {packagePath}", packagePath));
            _logger.LogError(ex, "Package file not found at '{PackagePath}'", packagePath);
            throw ex;
        }

        try
        {
            // 计算包哈希
            await using var stream = File.OpenRead(packagePath);
            var packageHash = await SHA256.HashDataAsync(stream);
            var packageHashBase64 = Convert.ToBase64String(packageHash);

            // 验证哈希值
            if (signature.PackageHash != packageHashBase64)
            {
                _logger.LogWarning("Package hash mismatch for '{PackagePath}'. Expected: '{Expected}', Actual: '{Actual}'",
                    packagePath, signature.PackageHash, packageHashBase64);
                return false;
            }

            // 从签名中提取公钥并验证
            var publicKeyPem = signature.Signer.PublicKey;
            using var cert = X509Certificate2.CreateFromPem(publicKeyPem);
            using var rsa = cert.GetRSAPublicKey();

            if (rsa == null)
            {
                _logger.LogWarning("Unable to extract RSA public key from signature for '{PackagePath}'", packagePath);
                return false;
            }

            var signatureBytes = Convert.FromBase64String(signature.SignatureData);
            var isValid = rsa.VerifyData(packageHash, signatureBytes, HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            if (isValid)
            {
                _logger.SignatureValid(packageId, signature.Version ?? "unknown", signature.Signer.Name ?? "Unknown");
            }
            else
            {
                _logger.SignatureInvalid(packageId, signature.Version ?? "unknown",
                    new PackageSignatureException("Signature verification failed", packageId, signature.Version));
            }

            return isValid;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error while verifying signature for '{PackagePath}'", packagePath);
            return false;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid signature format for '{PackagePath}'", packagePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while verifying signature for '{PackagePath}'", packagePath);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<PackageSignature?> ReadSignatureAsync(string signatureFilePath)
    {
        if (!File.Exists(signatureFilePath))
        {
            _logger.LogDebug("Signature file not found at '{SignatureFilePath}'", signatureFilePath);
            return null;
        }

        try
        {
            var signatureJson = await File.ReadAllTextAsync(signatureFilePath);
            var signature = JsonSerializer.Deserialize<PackageSignature>(signatureJson);
            _logger.LogDebug("Successfully read signature from '{SignatureFilePath}'", signatureFilePath);
            return signature;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse signature file '{SignatureFilePath}'", signatureFilePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error reading signature file '{SignatureFilePath}'", signatureFilePath);
            return null;
        }
    }

    /// <summary>
    /// 写入签名
    /// </summary>
    /// <param name="signature">签名信息</param>
    /// <param name="signatureFilePath">签名文件路径</param>
    public async Task WriteSignatureAsync(PackageSignature signature, string signatureFilePath)
    {
        try
        {
            var signatureJson = JsonSerializer.Serialize(signature, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(signatureFilePath, signatureJson);
            _logger.LogInformation("Successfully wrote signature to '{SignatureFilePath}'", signatureFilePath);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while writing signature to '{SignatureFilePath}'", signatureFilePath);
            throw new PackageSignatureException(
                $"Failed to write signature file: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while writing signature to '{SignatureFilePath}'", signatureFilePath);
            throw new PackageSignatureException(
                $"Failed to write signature file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 生成自签名证书
    /// </summary>
    /// <param name="subjectName"></param>
    /// <param name="email"></param>
    /// <param name="validityYears"></param>
    /// <returns></returns>
    public X509Certificate2 GenerateSelfSignedCertificate(string subjectName, string? email = null,
        int validityYears = 5)
    {
        using var rsa = RSA.Create(DefaultRsaKeySize);

        var distinguishedName = string.IsNullOrEmpty(email)
            ? $"CN={subjectName}"
            : $"CN={subjectName}, E={email}";

        var request = new CertificateRequest(
            distinguishedName,
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
            DateTimeOffset.UtcNow.AddYears(validityYears));

        return certificate;
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2> LoadCertificateAsync(string certPath, string? password = null)
    {
        if (!File.Exists(certPath))
        {
            throw new FileNotFoundException($"Certificate file not found: {certPath}", certPath);
        }

        var certBytes = await File.ReadAllBytesAsync(certPath);

        return string.IsNullOrEmpty(password)
            ? X509CertificateLoader.LoadCertificate(certBytes)
            : X509CertificateLoader.LoadPkcs12(certBytes, password);
    }

    /// <summary>
    /// 导出证书
    /// </summary>
    /// <param name="certificate"></param>
    /// <param name="outputPath"></param>
    /// <param name="password"></param>
    public async Task ExportCertificateAsync(X509Certificate2 certificate, string outputPath, string? password = null)
    {
        var exportBytes = string.IsNullOrEmpty(password)
            ? certificate.Export(X509ContentType.Cert)
            : certificate.Export(X509ContentType.Pfx, password);

        await File.WriteAllBytesAsync(outputPath, exportBytes);
    }

    /// <summary>
    /// 获取证书信息
    /// </summary>
    /// <param name="certificate"></param>
    /// <returns></returns>
    public string GetCertificateInfo(X509Certificate2 certificate)
    {
        var status = certificate.NotAfter < DateTime.UtcNow ? "EXPIRED" : "Valid";

        return $@"Certificate Information:
Subject: {certificate.Subject}
Issuer: {certificate.Issuer}
Thumbprint: {certificate.Thumbprint}
Valid From: {certificate.NotBefore:yyyy-MM-dd HH:mm:ss}
Valid Until: {certificate.NotAfter:yyyy-MM-dd HH:mm:ss}
Has Private Key: {certificate.HasPrivateKey}
Status: {status}";
    }
}