using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Old8Lang.PackageManager.Server.Data;
using Old8Lang.PackageManager.Server.Models;
using Old8Lang.PackageManager.Server.Services;

namespace Old8Lang.PackageManager.Tests.UnitTests;

/// <summary>
/// 多语言包管理服务单元测试
/// </summary>
public class MultiLanguagePackageManagementServiceTests
{
    private readonly Mock<PackageManagerDbContext> _mockDbContext;
    private readonly Mock<IPackageStorageService> _mockStorageService;
    private readonly Mock<IPythonPackageParser> _mockPythonParser;
    private readonly Mock<ILogger<PackageManagementService>> _mockLogger;
    private readonly PackageManagementService _service;

    public MultiLanguagePackageManagementServiceTests()
    {
        _mockDbContext = new Mock<PackageManagerDbContext>();
        _mockStorageService = new Mock<IPackageStorageService>();
        _mockPythonParser = new Mock<IPythonPackageParser>();
        _mockLogger = new Mock<ILogger<PackageManagementService>>();
        
        _service = new PackageManagementService(
            _mockDbContext.Object,
            _mockStorageService.Object,
            _mockLogger.Object,
            _mockPythonParser.Object);
    }

    [Theory]
    [InlineData("test-package", "1.0.0", "old8lang")]
    [InlineData("requests", "2.28.0", "python")]
    [InlineData("numpy", "1.21.0", "python")]
    public async Task GetPackageAsync_ShouldReturnPackageWithCorrectLanguage(string packageId, string version, string language)
    {
        // Arrange
        var package = CreateTestPackage(packageId, version, language);
        var packages = new List<PackageEntity> { package }.AsQueryable();
        
        var mockSet = new Mock<DbSet<PackageEntity>>();
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.Provider).Returns(packages.Provider);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.Expression).Returns(packages.Expression);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.ElementType).Returns(packages.ElementType);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.GetEnumerator()).Returns(packages.GetEnumerator());
        
        _mockDbContext.Setup(c => c.Packages).Returns(mockSet.Object);
        _mockDbContext.Setup(c => c.Packages.Include(It.IsAny<string>())).Returns(mockSet.Object);

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
        var packages = new List<PackageEntity>
        {
            CreateTestPackage("test-package", "1.0.0", "old8lang"),
            CreateTestPackage("test-package", "1.0.0", "python"),
            CreateTestPackage("other-package", "1.0.0", language)
        }.AsQueryable();

        var mockSet = new Mock<DbSet<PackageEntity>>();
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.Provider).Returns(packages.Provider);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.Expression).Returns(packages.Expression);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.ElementType).Returns(packages.ElementType);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.GetEnumerator()).Returns(packages.GetEnumerator());

        _mockDbContext.Setup(c => c.Packages).Returns(mockSet.Object);
        _mockDbContext.Setup(c => c.Packages.Include(It.IsAny<string>())).Returns(mockSet.Object);

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
        var packages = new List<PackageEntity>
        {
            CreateTestPackage("popular-py", "1.0.0", "python", downloadCount: 1000),
            CreateTestPackage("popular-o8", "1.0.0", "old8lang", downloadCount: 800),
            CreateTestPackage("less-popular", "1.0.0", "python", downloadCount: 100)
        }.AsQueryable();

        var mockSet = new Mock<DbSet<PackageEntity>>();
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.Provider).Returns(packages.Provider);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.Expression).Returns(packages.Expression);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.ElementType).Returns(packages.ElementType);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.GetEnumerator()).Returns(packages.GetEnumerator());

        _mockDbContext.Setup(c => c.Packages).Returns(mockSet.Object);
        _mockDbContext.Setup(c => c.Packages.Include(It.IsAny<string>())).Returns(mockSet.Object);

        // Act
        var pythonResult = await _service.GetPopularPackagesAsync("python");
        var old8langResult = await _service.GetPopularPackagesAsync("old8lang");

        // Assert
        pythonResult.Should().HaveCount(2);
        pythonResult.Should().Contain(p => p.PackageId == "popular-py");
        pythonResult.Should().Contain(p => p.PackageId == "less-popular");

        old8langResult.Should().HaveCount(1);
        old8langResult.Should().Contain(p => p.PackageId == "popular-o8");
    }

    [Fact]
    public async Task UploadPackageAsync_ShouldHandlePythonPackageUpload()
    {
        // Arrange
        var packageFile = new Mock<IFormFile>();
        packageFile.Setup(f => f.FileName).Returns("test-package-1.0.0-py3-none-any.whl");
        packageFile.Setup(f => f.Length).Returns(1024L);

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

        var packageStream = new MemoryStream();
        _mockPythonParser.Setup(p => p.ParsePackageAsync(It.IsAny<Stream>(), "test-package-1.0.0-py3-none-any.whl"))
                      .ReturnsAsync(pythonInfo);

        _mockStorageService.Setup(s => s.StorePackageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("/test/path");

        _mockStorageService.Setup(s => s.CalculateChecksumAsync(It.IsAny<string>()))
                .ReturnsAsync("test-checksum");

        _mockStorageService.Setup(s => s.GetPackageSizeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(1024L);

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

        var mockSet = new Mock<DbSet<PackageEntity>>();
        _mockDbContext.Setup(c => c.Packages).Returns(mockSet.Object);
        _mockDbContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

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
        var packageFile = new Mock<IFormFile>();
        packageFile.Setup(f => f.FileName).Returns("test-package-1.0.0.o8pkg");
        packageFile.Setup(f => f.Length).Returns(1024L);

        var packageStream = new MemoryStream();
        _mockStorageService.Setup(s => s.StorePackageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync("/test/path");

        _mockStorageService.Setup(s => s.CalculateChecksumAsync(It.IsAny<string>()))
                .ReturnsAsync("test-checksum");

        _mockStorageService.Setup(s => s.GetPackageSizeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(1024L);

        var request = new PackageUploadRequest
        {
            PackageFile = packageFile.Object,
            Language = "old8lang",
            Author = "Test Author",
            Description = "Test Description"
        };

        var mockSet = new Mock<DbSet<PackageEntity>>();
        _mockDbContext.Setup(c => c.Packages).Returns(mockSet.Object);
        _mockDbContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

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
        // Arrange
        var existingPackage = expectedExists ? CreateTestPackage(packageId, version, language) : null;
        var packages = existingPackage != null ? 
            new List<PackageEntity> { existingPackage }.AsQueryable() : 
            new List<PackageEntity>().AsQueryable();

        var mockSet = new Mock<DbSet<PackageEntity>>();
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.Provider).Returns(packages.Provider);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.Expression).Returns(packages.Expression);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.ElementType).Returns(packages.ElementType);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.GetEnumerator()).Returns(packages.GetEnumerator());

        _mockDbContext.Setup(c => c.Packages).Returns(mockSet.Object);

        // Act
        var result = await _service.PackageExistsAsync(packageId, version, language);

        // Assert
        result.Should().Be(expectedExists);
    }

    [Fact]
    public async Task GetAllVersionsAsync_ShouldReturnAllVersionsForPackage()
    {
        // Arrange
        var packages = new List<PackageEntity>
        {
            CreateTestPackage("test-package", "1.0.0", "python"),
            CreateTestPackage("test-package", "1.1.0", "python"),
            CreateTestPackage("test-package", "1.0.0", "old8lang"),
            CreateTestPackage("test-package", "2.0.0", "python")
        }.AsQueryable();

        var mockSet = new Mock<DbSet<PackageEntity>>();
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.Provider).Returns(packages.Provider);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.Expression).Returns(packages.Expression);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.ElementType).Returns(packages.ElementType);
        mockSet.As<IQueryable<PackageEntity>>().Setup(m => m.GetEnumerator()).Returns(packages.GetEnumerator());

        _mockDbContext.Setup(c => c.Packages).Returns(mockSet.Object);
        _mockDbContext.Setup(c => c.Packages.Include(It.IsAny<string>())).Returns(mockSet.Object);

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
        // This would test a private method, for demonstration we'll use the parser directly
        var result = _mockPythonParser.Object.GetLanguageFromExtension(fileName);

        // Act & Assert - this tests the parser's language detection
        result.Should().Be(expectedLanguage);
    }

    private PackageEntity CreateTestPackage(string packageId, string version, string language, long downloadCount = 0)
    {
        return new PackageEntity
        {
            Id = 1,
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
            DownloadCount = downloadCount,
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