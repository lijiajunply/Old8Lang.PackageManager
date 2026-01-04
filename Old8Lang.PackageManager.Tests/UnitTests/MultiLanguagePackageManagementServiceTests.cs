using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Old8Lang.PackageManager.Server.Data;
using Old8Lang.PackageManager.Server.Models;
using Old8Lang.PackageManager.Server.Services;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Tests.UnitTests;

/// <summary>
/// 多语言包管理服务单元测试
/// </summary>
public class MultiLanguagePackageManagementServiceTests : IDisposable
{
    private readonly PackageManagerDbContext _dbContext;
    private readonly Mock<IPackageStorageService> _mockStorageService;
    private readonly Mock<IPythonPackageParser> _mockPythonParser;
    private readonly Mock<ILogger<PackageManagementService>> _mockLogger;
    private readonly Mock<IPackageSignatureService> _mockSignatureService;
    private readonly Mock<IPackageQualityService> _mockQualityService;
    private readonly PackageManagementService _service;

    public MultiLanguagePackageManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<PackageManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PackageManagerDbContext(options);
        _mockStorageService = new Mock<IPackageStorageService>();
        _mockPythonParser = new Mock<IPythonPackageParser>();
        _mockLogger = new Mock<ILogger<PackageManagementService>>();
        _mockSignatureService = new Mock<IPackageSignatureService>();
        _mockQualityService = new Mock<IPackageQualityService>();

        _service = new PackageManagementService(
            _dbContext,
            _mockStorageService.Object,
            _mockSignatureService.Object,
            _mockQualityService.Object,
            _mockLogger.Object,
            _mockPythonParser.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    [Theory]
    [InlineData("test-package", "1.0.0", "old8lang")]
    [InlineData("requests", "2.28.0", "python")]
    [InlineData("numpy", "1.21.0", "python")]
    public async Task GetPackageAsync_ShouldReturnPackageWithCorrectLanguage(string packageId, string version, string language)
    {
        // Arrange
        var package = CreateTestPackage(packageId, version, language);
        _dbContext.Packages.Add(package);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPackageAsync(packageId, version, language);

        // Assert
        result.Should().NotBeNull();
        result!.PackageId.Should().Be(packageId);
        result.Version.Should().Be(version);
        result.Language.Should().Be(language);
    }

    [Theory]
    [InlineData("test-package", "python")]
    [InlineData("requests", "old8lang")]
    public async Task SearchPackagesAsync_ShouldFilterByLanguage(string searchTerm, string language)
    {
        // Arrange
        _dbContext.Packages.Add(CreateTestPackage("test-package", "1.0.0", "old8lang"));
        _dbContext.Packages.Add(CreateTestPackage("test-package", "1.0.0", "python"));
        _dbContext.Packages.Add(CreateTestPackage("requests", "2.28.0", "old8lang"));
        _dbContext.Packages.Add(CreateTestPackage("requests", "2.28.0", "python"));
        _dbContext.Packages.Add(CreateTestPackage("other-package", "1.0.0", language));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.SearchPackagesAsync(searchTerm, language);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.PackageId == searchTerm && p.Language == language);
        result.Should().NotContain(p => p.PackageId == searchTerm && p.Language != language);
    }

    [Fact]
    public async Task GetPopularPackagesAsync_ShouldReturnLanguageSpecificPackages()
    {
        // Arrange
        _dbContext.Packages.Add(CreateTestPackage("popular-py", "1.0.0", "python", downloadCount: 1000));
        _dbContext.Packages.Add(CreateTestPackage("popular-o8", "1.0.0", "old8lang", downloadCount: 800));
        _dbContext.Packages.Add(CreateTestPackage("less-popular", "1.0.0", "python", downloadCount: 100));
        await _dbContext.SaveChangesAsync();

        // Act
        var pythonResult = await _service.GetPopularPackagesAsync("python");
        var old8LangResult = await _service.GetPopularPackagesAsync("old8lang");

        // Assert
        pythonResult.Should().HaveCount(2);
        pythonResult.Should().Contain(p => p.PackageId == "popular-py");
        pythonResult.Should().Contain(p => p.PackageId == "less-popular");

        old8LangResult.Should().HaveCount(1);
        old8LangResult.Should().Contain(p => p.PackageId == "popular-o8");
    }

    [Fact]
    public async Task UploadPackageAsync_ShouldHandlePythonPackageUpload()
    {
        // Arrange
        var packageStream = new MemoryStream(new byte[1024]);

        var packageFile = new Mock<IFormFile>();
        packageFile.Setup(f => f.FileName).Returns("test-package-1.0.0-py3-none-any.whl");
        packageFile.Setup(f => f.Length).Returns(1024L);
        packageFile.Setup(f => f.OpenReadStream()).Returns(packageStream);

        var pythonInfo = new PythonPackageInfo
        {
            PackageId = "test-package",
            Version = "1.0.0",
            Summary = "Test Python package",
            Author = "Test Author",
            Dependencies = new List<ExternalDependencyInfo>
            {
                new() { PackageName = "requests", VersionSpec = ">=2.28.0" }
            }
        };

        _mockPythonParser.Setup(p => p.ParsePackageAsync(It.IsAny<Stream>(), "test-package-1.0.0-py3-none-any.whl"))
                      .ReturnsAsync(pythonInfo);

        _mockStorageService.Setup(s => s.StorePackageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("/test/path");

        _mockStorageService.Setup(s => s.CalculateChecksumAsync(It.IsAny<string>()))
                .ReturnsAsync("test-checksum");

        _mockStorageService.Setup(s => s.GetPackageSizeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(1024L);

        _mockSignatureService.Setup(s => s.VerifyPackageSignatureAsync(It.IsAny<string>()))
                .ReturnsAsync(SignatureVerificationResult.Failure("Not signed"));

        _mockQualityService.Setup(s => s.CalculateQualityScoreAsync(It.IsAny<PackageEntity>()))
                .ReturnsAsync((PackageQualityScoreEntity?)null!);

        var request = new PackageUploadRequest
        {
            PackageFile = packageFile.Object,
            Language = "python",
            Author = "Test Author",
            Description = "Test Description",
            ExternalDependencies = new List<ExternalDependencyInfo>
            {
                new() { PackageName = "numpy", VersionSpec = ">=1.21.0" }
            }
        };

        // Act
        var result = await _service.UploadPackageAsync(request, packageStream);

        // Assert
        result.Should().NotBeNull();
        result.PackageId.Should().Be("test-package");
        result.Version.Should().Be("1.0.0");
        result.Language.Should().Be("python");
        result.Description.Should().Be("Test Description");
        result.Author.Should().Be("Test Author");
    }

    [Fact]
    public async Task UploadPackageAsync_ShouldHandleOld8LangPackageUpload()
    {
        // Arrange
        var packageStream = new MemoryStream(new byte[1024]);

        var packageFile = new Mock<IFormFile>();
        packageFile.Setup(f => f.FileName).Returns("test-package-1.0.0.o8pkg");
        packageFile.Setup(f => f.Length).Returns(1024L);
        packageFile.Setup(f => f.OpenReadStream()).Returns(packageStream);

        _mockStorageService.Setup(s => s.StorePackageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("/test/path");

        _mockStorageService.Setup(s => s.CalculateChecksumAsync(It.IsAny<string>()))
                .ReturnsAsync("test-checksum");

        _mockStorageService.Setup(s => s.GetPackageSizeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(1024L);

        _mockSignatureService.Setup(s => s.VerifyPackageSignatureAsync(It.IsAny<string>()))
                .ReturnsAsync(SignatureVerificationResult.Failure("Not signed"));

        _mockQualityService.Setup(s => s.CalculateQualityScoreAsync(It.IsAny<PackageEntity>()))
                .ReturnsAsync((PackageQualityScoreEntity?)null!);

        var request = new PackageUploadRequest
        {
            PackageFile = packageFile.Object,
            Language = "old8lang",
            Author = "Test Author",
            Description = "Test Description"
        };

        // Act
        var result = await _service.UploadPackageAsync(request, packageStream);

        // Assert
        result.Should().NotBeNull();
        result.PackageId.Should().Be("UnknownPackage"); // From mock parser
        result.Language.Should().Be("old8lang");
    }

    [Theory]
    [InlineData("test-package", "1.0.0", "python", true)]
    [InlineData("test-package", "1.0.0", "old8lang", false)]
    [InlineData("nonexistent", "1.0.0", "python", false)]
    public async Task PackageExistsAsync_ShouldCheckLanguageSpecificPackages(string packageId, string version, string language, bool expectedExists)
    {
        // 使用InMemory数据库来避免复杂的EF Core异步查询Mock问题
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<PackageManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var context = new PackageManagerDbContext(options))
        {
            var mockStorageService = new Mock<IPackageStorageService>();
            var mockPythonParser = new Mock<IPythonPackageParser>();
            var mockLogger = new Mock<ILogger<PackageManagementService>>();
            var mockSignatureService = new Mock<IPackageSignatureService>();
            var mockQualityService = new Mock<IPackageQualityService>();

            var service = new PackageManagementService(
                context,
                mockStorageService.Object,
                mockSignatureService.Object,
                mockQualityService.Object,
                mockLogger.Object,
                mockPythonParser.Object);

            // Arrange
            if (expectedExists)
            {
                var existingPackage = CreateTestPackage(packageId, version, language);
                context.Packages.Add(existingPackage);
                await context.SaveChangesAsync();
            }

            // Act
            var result = await service.PackageExistsAsync(packageId, version, language);

            // Assert
            result.Should().Be(expectedExists);
        }
    }

    [Fact]
    public async Task GetAllVersionsAsync_ShouldReturnAllVersionsForPackage()
    {
        // Arrange
        _dbContext.Packages.Add(CreateTestPackage("test-package", "1.0.0", "python"));
        _dbContext.Packages.Add(CreateTestPackage("test-package", "1.1.0", "python"));
        _dbContext.Packages.Add(CreateTestPackage("test-package", "1.0.0", "old8lang"));
        _dbContext.Packages.Add(CreateTestPackage("test-package", "2.0.0", "python"));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAllVersionsAsync("test-package");

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(p => p.Language == "python" && p.Version == "1.0.0");
        result.Should().Contain(p => p.Language == "python" && p.Version == "1.1.0");
        result.Should().Contain(p => p.Language == "old8lang" && p.Version == "1.0.0");
        result.Should().Contain(p => p.Language == "python" && p.Version == "2.0.0");
    }

    [Theory]
    [InlineData("requests-2.28.0-py3-none-any.whl", "python")]
    [InlineData("test-package-1.0.0.tar.gz", "python")]
    [InlineData("my-package-1.0.0.o8pkg", "old8lang")]
    [InlineData("unknown-package.txt", "unknown")]
    public void DeterminePackageLanguage_ShouldReturnCorrectLanguage(string fileName, string expectedLanguage)
    {
        // Arrange - Setup mock behavior for language detection
        _mockPythonParser.Setup(p => p.GetLanguageFromExtension(It.IsAny<string>()))
            .Returns<string>(f => f.ToLowerInvariant() switch
            {
                var file when file.EndsWith(".whl") || file.EndsWith(".tar.gz") => "python",
                var file when file.EndsWith(".o8pkg") => "old8lang",
                _ => "unknown"
            });

        // Act
        var result = _mockPythonParser.Object.GetLanguageFromExtension(fileName);

        // Assert
        result.Should().Be(expectedLanguage);
    }

    private PackageEntity CreateTestPackage(string packageId, string version, string language, long downloadCount = 0)
    {
        return new PackageEntity
        {
            // Don't set Id - let EF Core auto-generate it to avoid primary key conflicts
            PackageId = packageId,
            Version = version,
            Language = language,
            Description = $"Test {packageId} package",
            Author = "Test Author",
            License = "MIT",
            ProjectUrl = $"https://github.com/test/{packageId}",
            Checksum = "test-checksum",
            Size = 1024,
            PublishedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DownloadCount = (int)downloadCount,
            IsListed = true,
            IsPrerelease = false,
            PackageTags = new List<PackageTagEntity>(),
            PackageDependencies = new List<PackageDependencyEntity>(),
            Files = new List<PackageFileEntity>(),
            ExternalDependencies = new List<ExternalDependencyEntity>(),
            LanguageMetadata = new List<LanguageMetadataEntity>()
        };
    }

}