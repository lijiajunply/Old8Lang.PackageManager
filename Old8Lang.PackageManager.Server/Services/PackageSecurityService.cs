using Old8Lang.PackageManager.Server.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// 包签名验证服务
/// </summary>
public interface IPackageSignatureService
{
    Task<bool> VerifyPackageSignatureAsync(string packagePath, string? expectedSignature = null);
    Task<string> GeneratePackageSignatureAsync(string packagePath, X509Certificate2? certificate = null);
    Task<bool> ValidateCertificateAsync(X509Certificate2 certificate);
    Task<List<string>> GetTrustedCertificatesAsync();
}

/// <summary>
/// 包签名验证服务实现
/// </summary>
public class PackageSignatureService(SecurityOptions securityOptions, ILogger<PackageSignatureService> logger)
    : IPackageSignatureService
{
    public async Task<bool> VerifyPackageSignatureAsync(string packagePath, string? expectedSignature = null)
    {
        try
        {
            if (!securityOptions.EnablePackageSigning)
            {
                logger.LogDebug("包签名验证已禁用，跳过验证: {PackagePath}", packagePath);
                return true;
            }
            
            if (!File.Exists(packagePath))
            {
                logger.LogError("包文件不存在: {PackagePath}", packagePath);
                return false;
            }
            
            // 计算包的哈希值
            var hash = await ComputePackageHashAsync(packagePath);
            
            // 如果提供了期望的签名，直接验证
            if (!string.IsNullOrEmpty(expectedSignature))
            {
                return VerifySignature(hash, expectedSignature);
            }
            
            // 否则检查包中是否包含签名文件
            var signatureFile = Path.ChangeExtension(packagePath, ".sig");
            if (File.Exists(signatureFile))
            {
                var storedSignature = await File.ReadAllTextAsync(signatureFile);
                return VerifySignature(hash, storedSignature);
            }
            
            logger.LogWarning("未找到包签名: {PackagePath}", packagePath);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "验证包签名失败: {PackagePath}", packagePath);
            return false;
        }
    }
    
    public async Task<string> GeneratePackageSignatureAsync(string packagePath, X509Certificate2? certificate = null)
    {
        try
        {
            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException("包文件不存在", packagePath);
            }
            
            // 计算包的哈希值
            var hash = await ComputePackageHashAsync(packagePath);
            
            // 如果没有提供证书，使用简单的哈希签名（实际应用中应使用真实证书）
            if (certificate == null)
            {
                // 简化的"签名"：基于哈希和时间戳
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var combined = $"{Convert.ToBase64String(hash)}:{timestamp}";
                var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
                return signature;
            }
            
            // 使用真实证书签名
            using var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new InvalidOperationException("证书不包含私钥");
            }
            
            var signatureBytes = rsa.SignData(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signatureBytes);
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
            
            // 检查证书是否在信任列表中
            if (securityOptions.TrustedCertificates.Any())
            {
                var thumbprint = certificate.Thumbprint?.ToUpperInvariant();
                var isTrusted = securityOptions.TrustedCertificates.Contains(thumbprint);
                
                if (!isTrusted)
                {
                    logger.LogWarning("证书不在信任列表中: {Thumbprint}", thumbprint);
                    return false;
                }
            }
            
            // 验证证书链
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            
            var isValid = chain.Build(certificate);
            
            if (!isValid)
            {
                var errors = chain.ChainStatus.Select(status => status.StatusInformation);
                logger.LogWarning("证书验证失败: {Thumbprint}, 错误: {Errors}", 
                    certificate.Thumbprint, string.Join(", ", errors));
            }
            
            return isValid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "验证证书失败: {Thumbprint}", certificate.Thumbprint);
            return false;
        }
    }
    
    public async Task<List<string>> GetTrustedCertificatesAsync()
    {
        return await Task.FromResult(securityOptions.TrustedCertificates.ToList());
    }
    
    private async Task<byte[]> ComputePackageHashAsync(string packagePath)
    {
        var algorithm = securityOptions.AllowedHashAlgorithms.FirstOrDefault() ?? "SHA256";
        
        await using var stream = File.OpenRead(packagePath);
        
        return algorithm.ToUpperInvariant() switch
        {
            "SHA256" => await SHA256.HashDataAsync(stream),
            "SHA512" => await SHA512.HashDataAsync(stream),
            _ => throw new NotSupportedException($"不支持的哈希算法: {algorithm}")
        };
    }
    
    private bool VerifySignature(byte[] hash, string signature)
    {
        try
        {
            var signatureBytes = Convert.FromBase64String(signature);
            
            // 简化的验证逻辑（实际应用中应使用证书验证）
            if (signatureBytes.Length < hash.Length)
            {
                return false;
            }
            
            // 检查签名是否包含哈希信息
            if (securityOptions.EnableChecksumValidation)
            {
                var signatureString = Encoding.UTF8.GetString(signatureBytes);
                if (signatureString.Contains(':'))
                {
                    var parts = signatureString.Split(':');
                    if (parts.Length == 2)
                    {
                        var storedHash = Convert.FromBase64String(parts[0]);
                        return storedHash.SequenceEqual(hash);
                    }
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "验证签名失败");
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