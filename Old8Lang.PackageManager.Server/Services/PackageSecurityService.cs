using Old8Lang.PackageManager.Server.Configuration;
using Old8Lang.PackageManager.Core.Models;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// 包签名验证服务
/// </summary>
public interface IPackageSignatureService
{
    Task<SignatureVerificationResult> VerifyPackageSignatureAsync(string packagePath);
    Task<PackageSignature> SignPackageAsync(string packagePath, X509Certificate2 certificate);
    Task<bool> ValidateCertificateAsync(X509Certificate2 certificate);
    Task<List<CertificateInfo>> GetTrustedCertificatesAsync();
    Task AddTrustedCertificateAsync(X509Certificate2 certificate);
    Task RemoveTrustedCertificateAsync(string thumbprint);
}

/// <summary>
/// 包签名验证服务实现
/// </summary>
public class PackageSignatureService(
    SecurityOptions securityOptions,
    ILogger<PackageSignatureService> logger,
    ICertificateStore certificateStore)
    : IPackageSignatureService
{
    private const string SignatureFileExtension = ".sig";

    public async Task<SignatureVerificationResult> VerifyPackageSignatureAsync(string packagePath)
    {
        try
        {
            if (!securityOptions.EnablePackageSigning)
            {
                logger.LogDebug("包签名验证已禁用: {PackagePath}", packagePath);
                return SignatureVerificationResult.Success(null!, isTrusted: true);
            }

            if (!File.Exists(packagePath))
            {
                return SignatureVerificationResult.Failure("包文件不存在", packagePath);
            }

            // 查找签名文件
            var signatureFile = packagePath + SignatureFileExtension;
            if (!File.Exists(signatureFile))
            {
                logger.LogWarning("未找到签名文件: {SignatureFile}", signatureFile);
                return SignatureVerificationResult.Failure("未找到签名文件");
            }

            // 读取并解析签名
            var signatureJson = await File.ReadAllTextAsync(signatureFile);
            var signature = JsonSerializer.Deserialize<PackageSignature>(signatureJson);

            if (signature == null)
            {
                return SignatureVerificationResult.Failure("签名文件格式无效");
            }

            // 计算包的哈希值
            var packageHash = await ComputePackageHashAsync(packagePath, signature.HashAlgorithm);
            var packageHashBase64 = Convert.ToBase64String(packageHash);

            // 验证哈希值是否匹配
            if (signature.PackageHash != packageHashBase64)
            {
                logger.LogWarning("包哈希值不匹配: 期望={Expected}, 实际={Actual}",
                    signature.PackageHash, packageHashBase64);
                return SignatureVerificationResult.Failure("包哈希值不匹配，文件可能已被篡改");
            }

            // 验证签名
            var isSignatureValid = await VerifySignatureAsync(packageHash, signature);
            if (!isSignatureValid)
            {
                return SignatureVerificationResult.Failure("签名验证失败");
            }

            // 检查证书是否在信任列表中
            var trustedCerts = await certificateStore.GetTrustedCertificatesAsync();
            var isTrusted = trustedCerts.Any(c => c.Thumbprint.Equals(
                signature.Signer.CertificateThumbprint,
                StringComparison.OrdinalIgnoreCase));

            if (!isTrusted && securityOptions.RequireTrustedCertificates)
            {
                logger.LogWarning("签名证书不在信任列表中: {Thumbprint}",
                    signature.Signer.CertificateThumbprint);
                return SignatureVerificationResult.Failure(
                    "签名证书不在信任列表中",
                    signature.Signer.CertificateThumbprint);
            }

            // 检查证书是否过期
            if (signature.Signer.NotAfter < DateTimeOffset.UtcNow)
            {
                logger.LogWarning("签名证书已过期: {Thumbprint}", signature.Signer.CertificateThumbprint);
                return SignatureVerificationResult.Failure("签名证书已过期");
            }

            logger.LogInformation("包签名验证成功: {PackagePath}, 签名者: {Signer}",
                packagePath, signature.Signer.Name ?? signature.Signer.Email ?? "Unknown");

            return SignatureVerificationResult.Success(signature, isTrusted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "验证包签名失败: {PackagePath}", packagePath);
            return SignatureVerificationResult.Failure($"验证失败: {ex.Message}");
        }
    }

    public async Task<PackageSignature> SignPackageAsync(string packagePath, X509Certificate2 certificate)
    {
        try
        {
            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException("包文件不存在", packagePath);
            }

            if (!certificate.HasPrivateKey)
            {
                throw new InvalidOperationException("证书不包含私钥，无法签名");
            }

            // 使用配置的哈希算法
            var hashAlgorithm = securityOptions.AllowedHashAlgorithms.FirstOrDefault() ?? "SHA256";
            var packageHash = await ComputePackageHashAsync(packagePath, hashAlgorithm);

            // 使用 RSA 私钥签名
            using var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new InvalidOperationException("无法获取 RSA 私钥");
            }

            var hashAlgorithmName = hashAlgorithm switch
            {
                "SHA256" => HashAlgorithmName.SHA256,
                "SHA512" => HashAlgorithmName.SHA512,
                _ => throw new NotSupportedException($"不支持的哈希算法: {hashAlgorithm}")
            };

            var signatureBytes = rsa.SignData(packageHash, hashAlgorithmName, RSASignaturePadding.Pkcs1);

            // 构建签名信息
            var signature = new PackageSignature
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

            // 保存签名文件
            var signatureFile = packagePath + SignatureFileExtension;
            var signatureJson = JsonSerializer.Serialize(signature, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(signatureFile, signatureJson);

            logger.LogInformation("包签名成功: {PackagePath}, 签名文件: {SignatureFile}",
                packagePath, signatureFile);

            return signature;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "生成包签名失败: {PackagePath}", packagePath);
            throw;
        }
    }

    public async Task<bool> ValidateCertificateAsync(X509Certificate2 certificate)
    {
        try
        {
            // 检查证书是否过期
            if (certificate.NotAfter < DateTime.UtcNow)
            {
                logger.LogWarning("证书已过期: {Thumbprint}", certificate.Thumbprint);
                return false;
            }

            if (certificate.NotBefore > DateTime.UtcNow)
            {
                logger.LogWarning("证书尚未生效: {Thumbprint}", certificate.Thumbprint);
                return false;
            }

            // 如果启用了证书链验证
            if (securityOptions.ValidateCertificateChain)
            {
                var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                var isValid = chain.Build(certificate);

                if (!isValid)
                {
                    var errors = chain.ChainStatus.Select(status => status.StatusInformation);
                    logger.LogWarning("证书链验证失败: {Thumbprint}, 错误: {Errors}",
                        certificate.Thumbprint, string.Join(", ", errors));
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "验证证书失败: {Thumbprint}", certificate.Thumbprint);
            return false;
        }
    }

    public async Task<List<CertificateInfo>> GetTrustedCertificatesAsync()
    {
        return await certificateStore.GetTrustedCertificatesAsync();
    }

    public async Task AddTrustedCertificateAsync(X509Certificate2 certificate)
    {
        var isValid = await ValidateCertificateAsync(certificate);
        if (!isValid)
        {
            throw new InvalidOperationException("证书验证失败，无法添加到信任列表");
        }

        await certificateStore.AddTrustedCertificateAsync(certificate);
        logger.LogInformation("已添加信任证书: {Thumbprint}, 主题: {Subject}",
            certificate.Thumbprint, certificate.Subject);
    }

    public async Task RemoveTrustedCertificateAsync(string thumbprint)
    {
        await certificateStore.RemoveTrustedCertificateAsync(thumbprint);
        logger.LogInformation("已移除信任证书: {Thumbprint}", thumbprint);
    }

    private async Task<byte[]> ComputePackageHashAsync(string packagePath, string algorithm)
    {
        await using var stream = File.OpenRead(packagePath);

        return algorithm.ToUpperInvariant() switch
        {
            "SHA256" => await SHA256.HashDataAsync(stream),
            "SHA512" => await SHA512.HashDataAsync(stream),
            _ => throw new NotSupportedException($"不支持的哈希算法: {algorithm}")
        };
    }

    private async Task<bool> VerifySignatureAsync(byte[] packageHash, PackageSignature signature)
    {
        try
        {
            // 从签名中提取公钥
            var publicKeyPem = signature.Signer.PublicKey;
            using var cert = X509Certificate2.CreateFromPem(publicKeyPem);
            using var rsa = cert.GetRSAPublicKey();

            if (rsa == null)
            {
                logger.LogError("无法从证书获取 RSA 公钥");
                return false;
            }

            // 解析签名数据
            var signatureBytes = Convert.FromBase64String(signature.SignatureData);

            // 确定哈希算法
            var hashAlgorithmName = signature.HashAlgorithm.ToUpperInvariant() switch
            {
                "SHA256" => HashAlgorithmName.SHA256,
                "SHA512" => HashAlgorithmName.SHA512,
                _ => throw new NotSupportedException($"不支持的哈希算法: {signature.HashAlgorithm}")
            };

            // 验证签名
            var isValid = rsa.VerifyData(packageHash, signatureBytes, hashAlgorithmName, RSASignaturePadding.Pkcs1);

            if (!isValid)
            {
                logger.LogWarning("RSA 签名验证失败");
            }

            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "验证 RSA 签名时发生错误");
            return false;
        }
    }
}

/// <summary>
/// 包完整性检查服务
/// </summary>
public interface IPackageIntegrityService
{
    Task<bool> ValidatePackageIntegrityAsync(string packagePath, string expectedChecksum);
    Task<string> CalculateChecksumAsync(string packagePath);
    Task<bool> ValidatePackageStructureAsync(string packagePath);
}

/// <summary>
/// 包完整性检查服务实现
/// </summary>
public class PackageIntegrityService : IPackageIntegrityService
{
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<PackageIntegrityService> _logger;
    
    public PackageIntegrityService(SecurityOptions securityOptions, ILogger<PackageIntegrityService> logger)
    {
        _securityOptions = securityOptions;
        _logger = logger;
    }
    
    public async Task<bool> ValidatePackageIntegrityAsync(string packagePath, string expectedChecksum)
    {
        try
        {
            if (!_securityOptions.EnableChecksumValidation)
            {
                return true;
            }
            
            if (!File.Exists(packagePath))
            {
                _logger.LogError("包文件不存在: {PackagePath}", packagePath);
                return false;
            }
            
            var actualChecksum = await CalculateChecksumAsync(packagePath);
            var isValid = string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
            
            if (!isValid)
            {
                _logger.LogWarning("包校验和不匹配: {PackagePath}, 期望: {Expected}, 实际: {Actual}", 
                    packagePath, expectedChecksum, actualChecksum);
            }
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证包完整性失败: {PackagePath}", packagePath);
            return false;
        }
    }
    
    public async Task<string> CalculateChecksumAsync(string packagePath)
    {
        var algorithm = _securityOptions.AllowedHashAlgorithms.FirstOrDefault() ?? "SHA256";
        
        await using var stream = File.OpenRead(packagePath);
        
        var hash = algorithm.ToUpperInvariant() switch
        {
            "SHA256" => await SHA256.HashDataAsync(stream),
            "SHA512" => await SHA512.HashDataAsync(stream),
            _ => throw new NotSupportedException($"不支持的哈希算法: {algorithm}")
        };
        
        return Convert.ToBase64String(hash);
    }
    
    public async Task<bool> ValidatePackageStructureAsync(string packagePath)
    {
        try
        {
            if (!File.Exists(packagePath))
            {
                return false;
            }
            
            // 检查文件大小
            var fileInfo = new FileInfo(packagePath);
            if (fileInfo.Length == 0)
            {
                return false;
            }
            
            // 检查文件头（验证是否为有效的 ZIP 文件）
            await using var stream = File.OpenRead(packagePath);
            var buffer = new byte[4];
            await stream.ReadExactlyAsync(buffer, 0, 4);
            
            // ZIP 文件头: 0x504B0304 (PK\x03\x04)
            var isZip = buffer[0] == 0x50 && buffer[1] == 0x4B && 
                       buffer[2] == 0x03 && buffer[3] == 0x04;
            
            if (!isZip)
            {
                _logger.LogWarning("包文件格式无效，不是有效的 ZIP 文件: {PackagePath}", packagePath);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证包结构失败: {PackagePath}", packagePath);
            return false;
        }
    }
}