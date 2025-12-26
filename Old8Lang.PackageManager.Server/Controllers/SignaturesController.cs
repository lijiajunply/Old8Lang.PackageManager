using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Server.Services;
using System.Security.Cryptography.X509Certificates;

namespace Old8Lang.PackageManager.Server.Controllers;

/// <summary>
/// 包签名和证书管理 API
/// </summary>
[ApiController]
[Route("api/v3/signatures")]
[Produces("application/json")]
public class SignaturesController : ControllerBase
{
    private readonly IPackageSignatureService _signatureService;
    private readonly ICertificateManagementService _certificateService;
    private readonly IPackageStorageService _storageService;
    private readonly ILogger<SignaturesController> _logger;

    public SignaturesController(
        IPackageSignatureService signatureService,
        ICertificateManagementService certificateService,
        IPackageStorageService storageService,
        ILogger<SignaturesController> logger)
    {
        _signatureService = signatureService;
        _certificateService = certificateService;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// 验证包签名
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <param name="version">包版本</param>
    /// <returns>签名验证结果</returns>
    [HttpGet("verify/{packageId}/{version}")]
    [ProducesResponseType(typeof(SignatureVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SignatureVerificationResult>> VerifyPackageSignature(
        string packageId,
        string version)
    {
        try
        {
            var packagePath = await _storageService.GetPackagePathAsync(packageId, version);
            if (packagePath == null)
            {
                return NotFound(new { message = $"包 {packageId} 版本 {version} 不存在" });
            }

            var result = await _signatureService.VerifyPackageSignatureAsync(packagePath);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证包签名失败: {PackageId} {Version}", packageId, version);
            return StatusCode(500, new { message = "验证失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 对包进行签名
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <param name="version">包版本</param>
    /// <param name="request">签名请求</param>
    /// <returns>签名信息</returns>
    [HttpPost("sign/{packageId}/{version}")]
    [Authorize]
    [ProducesResponseType(typeof(PackageSignature), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageSignature>> SignPackage(
        string packageId,
        string version,
        [FromBody] SignPackageRequest request)
    {
        try
        {
            var packagePath = await _storageService.GetPackagePathAsync(packageId, version);
            if (packagePath == null)
            {
                return NotFound(new { message = $"包 {packageId} 版本 {version} 不存在" });
            }

            // 加载签名证书
            var certificate = await _certificateService.LoadCertificateForSigningAsync(
                request.CertificateThumbprint,
                request.CertificatePassword);

            // 验证证书
            var isValid = await _signatureService.ValidateCertificateAsync(certificate);
            if (!isValid)
            {
                return BadRequest(new { message = "证书验证失败" });
            }

            // 签名包
            var signature = await _signatureService.SignPackageAsync(packagePath, certificate);

            _logger.LogInformation("包已签名: {PackageId} {Version}, 证书: {Thumbprint}",
                packageId, version, request.CertificateThumbprint);

            return Ok(signature);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "签名包失败: {PackageId} {Version}", packageId, version);
            return StatusCode(500, new { message = "签名失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取可信任证书列表
    /// </summary>
    /// <returns>证书列表</returns>
    [HttpGet("certificates/trusted")]
    [ProducesResponseType(typeof(List<CertificateInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CertificateInfo>>> GetTrustedCertificates()
    {
        var certificates = await _signatureService.GetTrustedCertificatesAsync();
        return Ok(certificates);
    }

    /// <summary>
    /// 添加可信任证书
    /// </summary>
    /// <param name="certificateFile">证书文件</param>
    /// <param name="password">证书密码（可选）</param>
    /// <returns>证书信息</returns>
    [HttpPost("certificates/trusted")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CertificateInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CertificateInfo>> AddTrustedCertificate(
        IFormFile certificateFile,
        [FromForm] string? password = null)
    {
        try
        {
            if (certificateFile == null || certificateFile.Length == 0)
            {
                return BadRequest(new { message = "请上传证书文件" });
            }

            using var memoryStream = new MemoryStream();
            await certificateFile.CopyToAsync(memoryStream);
            var certificateData = memoryStream.ToArray();

            var certInfo = await _certificateService.ImportCertificateAsync(certificateData, password);

            _logger.LogInformation("已添加可信任证书: {Thumbprint}, 主题: {Subject}",
                certInfo.Thumbprint, certInfo.Subject);

            return Ok(certInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加可信任证书失败");
            return BadRequest(new { message = "导入证书失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 移除可信任证书
    /// </summary>
    /// <param name="thumbprint">证书指纹</param>
    /// <returns>操作结果</returns>
    [HttpDelete("certificates/trusted/{thumbprint}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTrustedCertificate(string thumbprint)
    {
        try
        {
            await _signatureService.RemoveTrustedCertificateAsync(thumbprint);

            _logger.LogInformation("已移除可信任证书: {Thumbprint}", thumbprint);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除可信任证书失败: {Thumbprint}", thumbprint);
            return NotFound(new { message = "证书不存在", error = ex.Message });
        }
    }

    /// <summary>
    /// 生成自签名证书
    /// </summary>
    /// <param name="request">证书生成请求</param>
    /// <returns>证书信息</returns>
    [HttpPost("certificates/generate")]
    [Authorize]
    [ProducesResponseType(typeof(GenerateCertificateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GenerateCertificateResponse>> GenerateCertificate(
        [FromBody] GenerateCertificateRequest request)
    {
        try
        {
            var certInfo = await _certificateService.GenerateCertificateAsync(
                request.SubjectName,
                request.Email,
                request.ValidityYears);

            _logger.LogInformation("已生成证书: {Thumbprint}, 主题: {Subject}",
                certInfo.Thumbprint, certInfo.Subject);

            return Ok(new GenerateCertificateResponse
            {
                Certificate = certInfo,
                Message = "证书生成成功。请妥善保管证书指纹，用于签名时使用。"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成证书失败");
            return BadRequest(new { message = "生成证书失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 导出证书
    /// </summary>
    /// <param name="thumbprint">证书指纹</param>
    /// <param name="includePrivateKey">是否包含私钥</param>
    /// <returns>证书文件</returns>
    [HttpGet("certificates/export/{thumbprint}")]
    [Authorize]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportCertificate(
        string thumbprint,
        [FromQuery] bool includePrivateKey = false)
    {
        try
        {
            var tempPath = Path.GetTempFileName();
            var extension = includePrivateKey ? ".pfx" : ".cer";
            var outputPath = Path.ChangeExtension(tempPath, extension);

            await _certificateService.ExportCertificateAsync(thumbprint, outputPath, includePrivateKey);

            var fileBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
            System.IO.File.Delete(outputPath);

            var contentType = includePrivateKey
                ? "application/x-pkcs12"
                : "application/x-x509-ca-cert";

            var fileName = $"certificate-{thumbprint}{extension}";

            return File(fileBytes, contentType, fileName);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出证书失败: {Thumbprint}", thumbprint);
            return StatusCode(500, new { message = "导出证书失败", error = ex.Message });
        }
    }
}

#region Request/Response Models

/// <summary>
/// 签名包请求
/// </summary>
public class SignPackageRequest
{
    /// <summary>
    /// 证书指纹
    /// </summary>
    public required string CertificateThumbprint { get; set; }

    /// <summary>
    /// 证书密码（可选）
    /// </summary>
    public string? CertificatePassword { get; set; }
}

/// <summary>
/// 生成证书请求
/// </summary>
public class GenerateCertificateRequest
{
    /// <summary>
    /// 主题名称 (CN)
    /// </summary>
    public required string SubjectName { get; set; }

    /// <summary>
    /// 电子邮箱（可选）
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 有效期（年）
    /// </summary>
    public int ValidityYears { get; set; } = 5;
}

/// <summary>
/// 生成证书响应
/// </summary>
public class GenerateCertificateResponse
{
    /// <summary>
    /// 证书信息
    /// </summary>
    public required CertificateInfo Certificate { get; set; }

    /// <summary>
    /// 提示消息
    /// </summary>
    public required string Message { get; set; }
}

#endregion
