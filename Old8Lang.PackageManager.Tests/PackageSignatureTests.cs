using FluentAssertions;
using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Server.Configuration;
using Old8Lang.PackageManager.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Old8Lang.PackageManager.Tests;

public class PackageSignatureTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testPackagePath;
    private readonly X509Certificate2 _testCertificate;
    private readonly Mock<ILogger<PackageSignatureService>> _loggerMock;
    private readonly Mock<ILogger<FileSystemCertificateStore>> _storeLoggerMock;
    private readonly SecurityOptions _securityOptions;
    private readonly PackageStorageOptions _storageOptions;

    public PackageSignatureTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "o8pm_signature_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        _testPackagePath = Path.Combine(_testDir, "test-package.o8pkg");
        File.WriteAllText(_testPackagePath, "This is a test package content");

        _testCertificate = GenerateTestCertificate("Test Package Signer");

        _loggerMock = new Mock<ILogger<PackageSignatureService>>();
        _storeLoggerMock = new Mock<ILogger<FileSystemCertificateStore>>();

        _securityOptions = new SecurityOptions
        {
            EnablePackageSigning = true,
            RequireTrustedCertificates = false,
            ValidateCertificateChain = false,
            EnableChecksumValidation = true,
            AllowedHashAlgorithms = new List<string> { "SHA256", "SHA512" }
        };

        _storageOptions = new PackageStorageOptions
        {
            StoragePath = _testDir
        };
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }

            _testCertificate?.Dispose();
        }
        catch
        {
            // Ignore cleanup errors
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SignPackageAsync_ShouldCreateSignatureFile()
    {
        // Arrange
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        var signatureService = new PackageSignatureService(_securityOptions, _loggerMock.Object, certificateStore);

        // Act
        var signature = await signatureService.SignPackageAsync(_testPackagePath, _testCertificate);

        // Assert
        signature.Should().NotBeNull();
        signature.Algorithm.Should().Be("RSA-SHA256");
        signature.SignatureData.Should().NotBeNullOrEmpty();
        signature.PackageHash.Should().NotBeNullOrEmpty();
        signature.Signer.Should().NotBeNull();
        signature.Signer.CertificateThumbprint.Should().Be(_testCertificate.Thumbprint);

        var signatureFile = _testPackagePath + ".sig";
        File.Exists(signatureFile).Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPackageSignatureAsync_WithValidSignature_ShouldSucceed()
    {
        // Arrange
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        var signatureService = new PackageSignatureService(_securityOptions, _loggerMock.Object, certificateStore);

        // Sign the package first
        await signatureService.SignPackageAsync(_testPackagePath, _testCertificate);

        // Act
        var result = await signatureService.VerifyPackageSignatureAsync(_testPackagePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Message.Should().Contain("成功");
        result.Signature.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyPackageSignatureAsync_WithTamperedPackage_ShouldFail()
    {
        // Arrange
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        var signatureService = new PackageSignatureService(_securityOptions, _loggerMock.Object, certificateStore);

        // Sign the package
        await signatureService.SignPackageAsync(_testPackagePath, _testCertificate);

        // Tamper with the package
        await File.AppendAllTextAsync(_testPackagePath, "TAMPERED");

        // Act
        var result = await signatureService.VerifyPackageSignatureAsync(_testPackagePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("哈希值不匹配");
    }

    [Fact]
    public async Task VerifyPackageSignatureAsync_WithoutSignatureFile_ShouldFail()
    {
        // Arrange
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        var signatureService = new PackageSignatureService(_securityOptions, _loggerMock.Object, certificateStore);

        // Act
        var result = await signatureService.VerifyPackageSignatureAsync(_testPackagePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("未找到签名文件");
    }

    [Fact]
    public async Task VerifyPackageSignatureAsync_WhenDisabled_ShouldSucceed()
    {
        // Arrange
        var options = new SecurityOptions { EnablePackageSigning = false };
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        var signatureService = new PackageSignatureService(options, _loggerMock.Object, certificateStore);

        // Act
        var result = await signatureService.VerifyPackageSignatureAsync(_testPackagePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateCertificateAsync_WithValidCertificate_ShouldSucceed()
    {
        // Arrange
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        var signatureService = new PackageSignatureService(_securityOptions, _loggerMock.Object, certificateStore);

        // Act
        var isValid = await signatureService.ValidateCertificateAsync(_testCertificate);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateCertificateAsync_WithExpiredCertificate_ShouldFail()
    {
        // Arrange
        var expiredCert = GenerateExpiredCertificate("Expired Test Certificate");
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        var signatureService = new PackageSignatureService(_securityOptions, _loggerMock.Object, certificateStore);

        // Act
        var isValid = await signatureService.ValidateCertificateAsync(expiredCert);

        // Assert
        isValid.Should().BeFalse();

        expiredCert.Dispose();
    }

    [Fact]
    public async Task CertificateStore_AddAndRetrieveCertificate_ShouldSucceed()
    {
        // Arrange
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);

        // Act
        await certificateStore.AddTrustedCertificateAsync(_testCertificate);
        var certificates = await certificateStore.GetTrustedCertificatesAsync();

        // Assert
        certificates.Should().NotBeEmpty();
        certificates.Should().Contain(c => c.Thumbprint == _testCertificate.Thumbprint);
    }

    [Fact]
    public async Task CertificateStore_RemoveCertificate_ShouldSucceed()
    {
        // Arrange
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        await certificateStore.AddTrustedCertificateAsync(_testCertificate);

        // Act
        await certificateStore.RemoveTrustedCertificateAsync(_testCertificate.Thumbprint);
        var certificates = await certificateStore.GetTrustedCertificatesAsync();

        // Assert
        certificates.Should().NotContain(c => c.Thumbprint == _testCertificate.Thumbprint);
    }

    [Fact]
    public async Task CertificateStore_GenerateSelfSignedCertificate_ShouldSucceed()
    {
        // Arrange
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);

        // Act
        var certificate = await certificateStore.GenerateSelfSignedCertificateAsync("Test Subject", 1);

        // Assert
        certificate.Should().NotBeNull();
        certificate.Subject.Should().Contain("Test Subject");
        certificate.HasPrivateKey.Should().BeTrue();
        certificate.NotAfter.Should().BeAfter(DateTime.UtcNow);

        certificate.Dispose();
    }

    [Fact]
    public async Task SignPackageAsync_WithSHA512_ShouldSucceed()
    {
        // Arrange
        var options = new SecurityOptions
        {
            EnablePackageSigning = true,
            AllowedHashAlgorithms = new List<string> { "SHA512" }
        };
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        var signatureService = new PackageSignatureService(options, _loggerMock.Object, certificateStore);

        // Act
        var signature = await signatureService.SignPackageAsync(_testPackagePath, _testCertificate);

        // Assert
        signature.Should().NotBeNull();
        signature.Algorithm.Should().Be("RSA-SHA512");
        signature.HashAlgorithm.Should().Be("SHA512");
    }

    [Fact]
    public async Task VerifyPackageSignatureAsync_WithRequiredTrustedCertificates_ShouldFail()
    {
        // Arrange
        var options = new SecurityOptions
        {
            EnablePackageSigning = true,
            RequireTrustedCertificates = true
        };
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        var signatureService = new PackageSignatureService(options, _loggerMock.Object, certificateStore);

        // Sign the package
        await signatureService.SignPackageAsync(_testPackagePath, _testCertificate);

        // Act - Verify without adding certificate to trusted list
        var result = await signatureService.VerifyPackageSignatureAsync(_testPackagePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.IsTrusted.Should().BeFalse();
        result.Message.Should().Contain("不在信任列表中");
    }

    [Fact]
    public async Task VerifyPackageSignatureAsync_WithTrustedCertificate_ShouldSucceed()
    {
        // Arrange
        var options = new SecurityOptions
        {
            EnablePackageSigning = true,
            RequireTrustedCertificates = true
        };
        var certificateStore = new FileSystemCertificateStore(_storageOptions, _storeLoggerMock.Object);
        var signatureService = new PackageSignatureService(options, _loggerMock.Object, certificateStore);

        // Add certificate to trusted list
        await certificateStore.AddTrustedCertificateAsync(_testCertificate);

        // Sign the package
        await signatureService.SignPackageAsync(_testPackagePath, _testCertificate);

        // Act
        var result = await signatureService.VerifyPackageSignatureAsync(_testPackagePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.IsTrusted.Should().BeTrue();
    }

    private X509Certificate2 GenerateTestCertificate(string subjectName)
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

    private X509Certificate2 GenerateExpiredCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            $"CN={subjectName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddYears(-2),
            DateTimeOffset.UtcNow.AddYears(-1));

        return certificate;
    }
}
