using System.Security.Cryptography.X509Certificates;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Core.Interfaces;

/// <summary>
/// 包签名服务接口
/// </summary>
public interface IPackageSignatureService
{
    /// <summary>
    /// 签名包文件
    /// </summary>
    /// <param name="packagePath">包文件路径</param>
    /// <param name="certificate">用于签名的证书</param>
    /// <returns>包签名信息</returns>
    Task<PackageSignature> SignPackageAsync(string packagePath, X509Certificate2 certificate);

    /// <summary>
    /// 验证包签名
    /// </summary>
    /// <param name="packagePath">包文件路径</param>
    /// <param name="signature">签名信息</param>
    /// <returns>签名是否有效</returns>
    Task<bool> VerifySignatureAsync(string packagePath, PackageSignature signature);

    /// <summary>
    /// 从文件读取签名
    /// </summary>
    /// <param name="signatureFilePath">签名文件路径</param>
    /// <returns>包签名信息</returns>
    Task<PackageSignature?> ReadSignatureAsync(string signatureFilePath);

    /// <summary>
    /// 将签名写入文件
    /// </summary>
    /// <param name="signature">签名信息</param>
    /// <param name="signatureFilePath">签名文件路径</param>
    Task WriteSignatureAsync(PackageSignature signature, string signatureFilePath);

    /// <summary>
    /// 生成自签名证书
    /// </summary>
    /// <param name="subjectName">证书主题名称</param>
    /// <param name="email">电子邮件（可选）</param>
    /// <param name="validityYears">有效期（年）</param>
    /// <returns>生成的证书</returns>
    X509Certificate2 GenerateSelfSignedCertificate(string subjectName, string? email = null, int validityYears = 5);

    /// <summary>
    /// 从文件加载证书
    /// </summary>
    /// <param name="certPath">证书文件路径</param>
    /// <param name="password">证书密码（可选）</param>
    /// <returns>证书</returns>
    Task<X509Certificate2> LoadCertificateAsync(string certPath, string? password = null);

    /// <summary>
    /// 导出证书到文件
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="password">密码（可选）</param>
    Task ExportCertificateAsync(X509Certificate2 certificate, string outputPath, string? password = null);

    /// <summary>
    /// 获取证书信息
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <returns>证书信息字符串</returns>
    string GetCertificateInfo(X509Certificate2 certificate);
}
