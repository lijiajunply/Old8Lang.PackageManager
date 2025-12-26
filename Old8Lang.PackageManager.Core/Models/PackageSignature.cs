using System.Security.Cryptography.X509Certificates;

namespace Old8Lang.PackageManager.Core.Models;

/// <summary>
/// 包签名信息
/// </summary>
public class PackageSignature
{
    /// <summary>
    /// 签名算法 (RSA-SHA256, RSA-SHA512)
    /// </summary>
    public required string Algorithm { get; init; }

    /// <summary>
    /// Base64 编码的签名数据
    /// </summary>
    public required string SignatureData { get; init; }

    /// <summary>
    /// 签名时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// 签名者信息
    /// </summary>
    public required SignerInfo Signer { get; init; }

    /// <summary>
    /// 包哈希值 (用于签名的原始哈希)
    /// </summary>
    public required string PackageHash { get; init; }

    /// <summary>
    /// 哈希算法
    /// </summary>
    public required string HashAlgorithm { get; init; }

    /// <summary>
    /// 签名版本
    /// </summary>
    public string Version { get; init; } = "1.0";
}

/// <summary>
/// 签名者信息
/// </summary>
public class SignerInfo
{
    /// <summary>
    /// 证书指纹 (SHA256)
    /// </summary>
    public required string CertificateThumbprint { get; init; }

    /// <summary>
    /// 签名者公钥 (PEM 格式)
    /// </summary>
    public required string PublicKey { get; init; }

    /// <summary>
    /// 签名者名称
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// 签名者邮箱
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// 证书有效期开始时间
    /// </summary>
    public DateTimeOffset NotBefore { get; init; }

    /// <summary>
    /// 证书有效期结束时间
    /// </summary>
    public DateTimeOffset NotAfter { get; init; }
}

/// <summary>
/// 签名验证结果
/// </summary>
public class SignatureVerificationResult
{
    /// <summary>
    /// 是否验证成功
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// 验证消息
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 签名信息 (如果存在)
    /// </summary>
    public PackageSignature? Signature { get; init; }

    /// <summary>
    /// 证书是否在信任列表中
    /// </summary>
    public bool IsTrusted { get; init; }

    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTimeOffset VerifiedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 错误详情 (如果有)
    /// </summary>
    public List<string> Errors { get; init; } = [];

    /// <summary>
    /// 验证成功
    /// </summary>
    /// <param name="signature"></param>
    /// <param name="isTrusted"></param>
    /// <returns></returns>
    public static SignatureVerificationResult Success(PackageSignature signature, bool isTrusted = true)
    {
        return new SignatureVerificationResult
        {
            IsValid = true,
            Message = "签名验证成功",
            Signature = signature,
            IsTrusted = isTrusted
        };
    }

    /// <summary>
    /// 验证失败
    /// </summary>
    /// <param name="message"></param>
    /// <param name="errors"></param>
    /// <returns></returns>
    public static SignatureVerificationResult Failure(string message, params string[] errors)
    {
        return new SignatureVerificationResult
        {
            IsValid = false,
            Message = message,
            Errors = errors.ToList()
        };
    }
}

/// <summary>
/// 证书信息
/// </summary>
public class CertificateInfo
{
    /// <summary>
    /// 证书指纹
    /// </summary>
    public required string Thumbprint { get; init; }

    /// <summary>
    /// 主题名称
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// 颁发者名称
    /// </summary>
    public required string Issuer { get; init; }

    /// <summary>
    /// 有效期开始
    /// </summary>
    public DateTimeOffset NotBefore { get; init; }

    /// <summary>
    /// 有效期结束
    /// </summary>
    public DateTimeOffset NotAfter { get; init; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired => NotAfter < DateTimeOffset.UtcNow;

    /// <summary>
    /// 是否已信任
    /// </summary>
    public bool IsTrusted { get; set; }

    /// <summary>
    /// 公钥 (PEM 格式)
    /// </summary>
    public required string PublicKey { get; init; }

    /// <summary>
    /// 从 X509Certificate2 创建证书信息
    /// </summary>
    /// <param name="cert"></param>
    /// <param name="isTrusted"></param>
    /// <returns></returns>
    public static CertificateInfo FromX509Certificate(X509Certificate2 cert, bool isTrusted = false)
    {
        return new CertificateInfo
        {
            Thumbprint = cert.Thumbprint,
            Subject = cert.Subject,
            Issuer = cert.Issuer,
            NotBefore = cert.NotBefore,
            NotAfter = cert.NotAfter,
            IsTrusted = isTrusted,
            PublicKey = cert.ExportCertificatePem()
        };
    }
}
