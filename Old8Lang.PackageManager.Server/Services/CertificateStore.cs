using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Server.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// 证书存储接口
/// </summary>
public interface ICertificateStore
{
    Task<List<CertificateInfo>> GetTrustedCertificatesAsync();
    Task AddTrustedCertificateAsync(X509Certificate2 certificate);
    Task RemoveTrustedCertificateAsync(string thumbprint);
    Task<X509Certificate2?> GetCertificateAsync(string thumbprint);
    Task<X509Certificate2> GenerateSelfSignedCertificateAsync(string subjectName, int validityYears = 5);
}

/// <summary>
/// 基于文件系统的证书存储实现
/// </summary>
public class FileSystemCertificateStore : ICertificateStore
{
    private readonly string _certificatesPath;
    private readonly ILogger<FileSystemCertificateStore> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileSystemCertificateStore(
        PackageStorageOptions storageOptions,
        ILogger<FileSystemCertificateStore> logger)
    {
        _certificatesPath = Path.Combine(storageOptions.StoragePath, "certificates");
        _logger = logger;

        // 确保证书目录存在
        Directory.CreateDirectory(_certificatesPath);
    }

    public async Task<List<CertificateInfo>> GetTrustedCertificatesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var certificates = new List<CertificateInfo>();
            var certFiles = Directory.GetFiles(_certificatesPath, "*.cer");

            foreach (var certFile in certFiles)
            {
                try
                {
                    var certBytes = await File.ReadAllBytesAsync(certFile);
                    using var cert = X509CertificateLoader.LoadCertificate(certBytes);
                    var certInfo = CertificateInfo.FromX509Certificate(cert, isTrusted: true);
                    certificates.Add(certInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "加载证书失败: {CertFile}", certFile);
                }
            }

            return certificates;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AddTrustedCertificateAsync(X509Certificate2 certificate)
    {
        await _lock.WaitAsync();
        try
        {
            var thumbprint = certificate.Thumbprint;
            var certPath = GetCertificatePath(thumbprint);

            if (File.Exists(certPath))
            {
                _logger.LogInformation("证书已存在: {Thumbprint}", thumbprint);
                return;
            }

            // 导出为 DER 格式
            var certBytes = certificate.Export(X509ContentType.Cert);
            await File.WriteAllBytesAsync(certPath, certBytes);

            // 保存证书元数据
            var metadataPath = Path.ChangeExtension(certPath, ".json");
            var certInfo = CertificateInfo.FromX509Certificate(certificate, isTrusted: true);
            var metadataJson = JsonSerializer.Serialize(certInfo, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(metadataPath, metadataJson);

            _logger.LogInformation("证书已添加: {Thumbprint}, 主题: {Subject}",
                thumbprint, certificate.Subject);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveTrustedCertificateAsync(string thumbprint)
    {
        await _lock.WaitAsync();
        try
        {
            var certPath = GetCertificatePath(thumbprint);
            var metadataPath = Path.ChangeExtension(certPath, ".json");

            if (File.Exists(certPath))
            {
                File.Delete(certPath);
                _logger.LogInformation("证书文件已删除: {Thumbprint}", thumbprint);
            }

            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<X509Certificate2?> GetCertificateAsync(string thumbprint)
    {
        await _lock.WaitAsync();
        try
        {
            var certPath = GetCertificatePath(thumbprint);
            if (!File.Exists(certPath))
            {
                return null;
            }

            var certBytes = await File.ReadAllBytesAsync(certPath);
            return X509CertificateLoader.LoadCertificate(certBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载证书失败: {Thumbprint}", thumbprint);
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<X509Certificate2> GenerateSelfSignedCertificateAsync(string subjectName, int validityYears = 5)
    {
        await _lock.WaitAsync();
        try
        {
            // 生成 RSA 密钥对
            using var rsa = RSA.Create(2048);

            // 创建证书请求
            var request = new CertificateRequest(
                $"CN={subjectName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // 添加扩展
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(
                    certificateAuthority: false,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: true));

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    critical: true));

            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));

            // 生成自签名证书
            var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(validityYears));

            // 保存证书和私钥
            var certPath = Path.Combine(_certificatesPath, $"{certificate.Thumbprint}.pfx");
            var certBytes = certificate.Export(X509ContentType.Pfx, "");
            await File.WriteAllBytesAsync(certPath, certBytes);

            _logger.LogInformation("生成自签名证书: {Thumbprint}, 主题: {Subject}",
                certificate.Thumbprint, certificate.Subject);

            return certificate;
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetCertificatePath(string thumbprint)
    {
        return Path.Combine(_certificatesPath, $"{thumbprint}.cer");
    }
}

/// <summary>
/// 证书管理服务
/// </summary>
public interface ICertificateManagementService
{
    Task<CertificateInfo> ImportCertificateAsync(byte[] certificateData, string? password = null);
    Task<CertificateInfo> GenerateCertificateAsync(string subjectName, string? email = null, int validityYears = 5);
    Task<X509Certificate2> LoadCertificateForSigningAsync(string thumbprint, string? password = null);
    Task ExportCertificateAsync(string thumbprint, string outputPath, bool includePrivateKey = false);
}

/// <summary>
/// 证书管理服务实现
/// </summary>
public class CertificateManagementService : ICertificateManagementService
{
    private readonly ICertificateStore _certificateStore;
    private readonly ILogger<CertificateManagementService> _logger;
    private readonly string _privateKeysPath;

    public CertificateManagementService(
        ICertificateStore certificateStore,
        PackageStorageOptions storageOptions,
        ILogger<CertificateManagementService> logger)
    {
        _certificateStore = certificateStore;
        _logger = logger;
        _privateKeysPath = Path.Combine(storageOptions.StoragePath, "private-keys");

        // 确保私钥目录存在
        Directory.CreateDirectory(_privateKeysPath);
    }

    public async Task<CertificateInfo> ImportCertificateAsync(byte[] certificateData, string? password = null)
    {
        try
        {
            X509Certificate2 certificate;

            if (string.IsNullOrEmpty(password))
            {
                certificate = X509CertificateLoader.LoadCertificate(certificateData);
            }
            else
            {
                certificate = X509CertificateLoader.LoadPkcs12(certificateData, password, X509KeyStorageFlags.Exportable);
            }

            // 如果包含私钥，保存私钥
            if (certificate.HasPrivateKey)
            {
                await SavePrivateKeyAsync(certificate);
            }

            await _certificateStore.AddTrustedCertificateAsync(certificate);

            _logger.LogInformation("导入证书: {Thumbprint}, 包含私钥: {HasPrivateKey}",
                certificate.Thumbprint, certificate.HasPrivateKey);

            return CertificateInfo.FromX509Certificate(certificate, isTrusted: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入证书失败");
            throw;
        }
    }

    public async Task<CertificateInfo> GenerateCertificateAsync(string subjectName, string? email = null, int validityYears = 5)
    {
        try
        {
            var fullSubjectName = string.IsNullOrEmpty(email)
                ? subjectName
                : $"{subjectName}, E={email}";

            var certificate = await _certificateStore.GenerateSelfSignedCertificateAsync(fullSubjectName, validityYears);

            // 保存私钥
            await SavePrivateKeyAsync(certificate);

            _logger.LogInformation("生成新证书: {Thumbprint}, 主题: {Subject}",
                certificate.Thumbprint, certificate.Subject);

            return CertificateInfo.FromX509Certificate(certificate, isTrusted: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成证书失败");
            throw;
        }
    }

    public async Task<X509Certificate2> LoadCertificateForSigningAsync(string thumbprint, string? password = null)
    {
        try
        {
            // 首先尝试从私钥存储加载
            var privateKeyPath = Path.Combine(_privateKeysPath, $"{thumbprint}.pfx");
            if (File.Exists(privateKeyPath))
            {
                var pfxBytes = await File.ReadAllBytesAsync(privateKeyPath);
                return X509CertificateLoader.LoadPkcs12(pfxBytes, password ?? "", X509KeyStorageFlags.Exportable);
            }

            // 否则从证书存储加载
            var certificate = await _certificateStore.GetCertificateAsync(thumbprint);
            if (certificate == null)
            {
                throw new FileNotFoundException($"未找到证书: {thumbprint}");
            }

            if (!certificate.HasPrivateKey)
            {
                throw new InvalidOperationException($"证书不包含私钥: {thumbprint}");
            }

            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载签名证书失败: {Thumbprint}", thumbprint);
            throw;
        }
    }

    public async Task ExportCertificateAsync(string thumbprint, string outputPath, bool includePrivateKey = false)
    {
        try
        {
            X509Certificate2? certificate;

            if (includePrivateKey)
            {
                certificate = await LoadCertificateForSigningAsync(thumbprint);
                var pfxBytes = certificate.Export(X509ContentType.Pfx, "");
                await File.WriteAllBytesAsync(outputPath, pfxBytes);
            }
            else
            {
                certificate = await _certificateStore.GetCertificateAsync(thumbprint);
                if (certificate == null)
                {
                    throw new FileNotFoundException($"未找到证书: {thumbprint}");
                }

                var cerBytes = certificate.Export(X509ContentType.Cert);
                await File.WriteAllBytesAsync(outputPath, cerBytes);
            }

            _logger.LogInformation("导出证书: {Thumbprint} -> {OutputPath}, 包含私钥: {IncludePrivateKey}",
                thumbprint, outputPath, includePrivateKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出证书失败: {Thumbprint}", thumbprint);
            throw;
        }
    }

    private async Task SavePrivateKeyAsync(X509Certificate2 certificate)
    {
        if (!certificate.HasPrivateKey)
        {
            return;
        }

        var privateKeyPath = Path.Combine(_privateKeysPath, $"{certificate.Thumbprint}.pfx");
        var pfxBytes = certificate.Export(X509ContentType.Pfx, "");
        await File.WriteAllBytesAsync(privateKeyPath, pfxBytes);

        _logger.LogDebug("保存私钥: {Thumbprint}", certificate.Thumbprint);
    }
}
