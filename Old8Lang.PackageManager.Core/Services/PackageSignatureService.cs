using Old8Lang.PackageManager.Core.Models;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Old8Lang.PackageManager.Core.Interfaces;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 包签名服务实现
/// </summary>
public class PackageSignatureService : IPackageSignatureService
{
    private const string DefaultHashAlgorithm = "SHA256";
    private const int DefaultRsaKeySize = 2048;

    /// <inheritdoc/>
    public async Task<PackageSignature> SignPackageAsync(string packagePath, X509Certificate2 certificate)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException($"Package file not found: {packagePath}", packagePath);
        }

        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException("Certificate does not contain a private key");
        }

        // 计算包哈希
        await using var stream = File.OpenRead(packagePath);
        var packageHash = await SHA256.HashDataAsync(stream);

        // 使用 RSA 私钥签名
        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa == null)
        {
            throw new InvalidOperationException("Unable to get RSA private key from certificate");
        }

        var signatureBytes = rsa.SignData(packageHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // 构建签名信息
        return new PackageSignature
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
    }

    /// <inheritdoc/>
    public async Task<bool> VerifySignatureAsync(string packagePath, PackageSignature signature)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException($"Package file not found: {packagePath}", packagePath);
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
                return false;
            }

            // 从签名中提取公钥并验证
            var publicKeyPem = signature.Signer.PublicKey;
            using var cert = X509Certificate2.CreateFromPem(publicKeyPem);
            using var rsa = cert.GetRSAPublicKey();

            if (rsa == null)
            {
                return false;
            }

            var signatureBytes = Convert.FromBase64String(signature.SignatureData);
            var isValid = rsa.VerifyData(packageHash, signatureBytes, HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return isValid;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<PackageSignature?> ReadSignatureAsync(string signatureFilePath)
    {
        if (!File.Exists(signatureFilePath))
        {
            return null;
        }

        var signatureJson = await File.ReadAllTextAsync(signatureFilePath);
        return JsonSerializer.Deserialize<PackageSignature>(signatureJson);
    }

    /// <inheritdoc/>
    public async Task WriteSignatureAsync(PackageSignature signature, string signatureFilePath)
    {
        var signatureJson = JsonSerializer.Serialize(signature, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(signatureFilePath, signatureJson);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task ExportCertificateAsync(X509Certificate2 certificate, string outputPath, string? password = null)
    {
        var exportBytes = string.IsNullOrEmpty(password)
            ? certificate.Export(X509ContentType.Cert)
            : certificate.Export(X509ContentType.Pfx, password);

        await File.WriteAllBytesAsync(outputPath, exportBytes);
    }

    /// <inheritdoc/>
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