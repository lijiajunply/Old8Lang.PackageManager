using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Old8Lang.PackageManager.Server.Controllers;
using Old8Lang.PackageManager.Server.Models;
using Old8Lang.PackageManager.Server.Services;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Old8Lang.PackageManager.Server.Configuration;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace Old8Lang.PackageManager.Tests.IntegrationTests;

/// <summary>
/// 多语言 API 控制器集成测试
/// </summary>
public class MultiLanguageApiControllerTests
{
    private readonly Mock<IPackageManagementService> _mockPackageService;
    private readonly Mock<IPackageSearchService> _mockSearchService;
    private readonly Mock<ILogger<PackagesController>> _mockLogger;
    private readonly Mock<IApiKeyService> _mockApiKeyService;
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly PackagesController _packagesController;
    private readonly ApiOptions _apiOptions;

    public MultiLanguageApiControllerTests()
    {
        _mockPackageService = new Mock<IPackageManagementService>();
        _mockSearchService = new Mock<IPackageSearchService>();
        _mockLogger = new Mock<ILogger<PackagesController>>();
        _mockApiKeyService = new Mock<IApiKeyService>();
        _mockLocalizationService = new Mock<ILocalizationService>();

        // 配置 ApiOptions 不需要 API 密钥
        _apiOptions = new ApiOptions
        {
            RequireApiKey = false,
            BaseUrl = "http://localhost:5000",
            Version = "3.0.0"
        };

        // 配置本地化服务返回默认消息
        _mockLocalizationService.Setup(l => l.GetString(It.IsAny<string>()))
            .Returns((string key) => key);

        _packagesController = new PackagesController(
            _mockSearchService.Object,
            _mockPackageService.Object,
            new Mock<IPackageQualityService>().Object,
            new Mock<IPackageDependencyService>().Object,
            _mockApiKeyService.Object,
            _apiOptions,
            _mockLocalizationService.Object,
            _mockLogger.Object);

        // 设置控制器上下文以绕过授权
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuthentication"));

        _packagesController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Theory]
    [InlineData("requests", "python")]
    [InlineData("numpy", "python")]
    [InlineData("my-package", "old8lang")]
    public async Task SearchPackages_WithLanguageFilter_ShouldReturnFilteredResults(string searchTerm, string language)
    {
        // Arrange
        var expectedResponse = new PackageSearchResponse
        {
            TotalHits = 1,
            Data = new List<PackageSearchResult>
            {
                new()
                {
                    PackageId = searchTerm,
                    Version = "1.0.0",
                    Language = language,
                    Description = $"Test {searchTerm} package"
                }
            }
        };

        _mockSearchService.Setup(s => s.SearchAsync(searchTerm, language, 0, 20))
                      .ReturnsAsync(expectedResponse);

        // Act
        var result = await _packagesController.SearchPackages(searchTerm, language, 0, 20);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        
        var responseValue = okResult!.Value as PackageSearchResponse;
        responseValue.Should().NotBeNull();
        responseValue!.TotalHits.Should().Be(1);
        responseValue.Data.Should().HaveCount(1);
        responseValue.Data.First().PackageId.Should().Be(searchTerm);
        responseValue.Data.First().Language.Should().Be(language);
    }

    [Theory]
    [InlineData("python", 10)]
    [InlineData("old8lang", 5)]
    public async Task GetPopularPackages_WithLanguageFilter_ShouldReturnFilteredResults(string language, int count)
    {
        // Arrange
        var expectedResponse = new PackageSearchResponse
        {
            TotalHits = count,
            Data = Enumerable.Range(1, count)
                .Select(i => new PackageSearchResult
                {
                    PackageId = $"package-{i}",
                    Language = language,
                    Description = $"Test package {i}"
                })
                .ToList()
        };

        _mockSearchService.Setup(s => s.GetPopularAsync(language, 20))
                      .ReturnsAsync(expectedResponse);

        // Act
        var result = await _packagesController.GetPopularPackages(language, 20);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        
        var responseValue = okResult!.Value as PackageSearchResponse;
        responseValue.Should().NotBeNull();
        responseValue!.TotalHits.Should().Be(count);
        responseValue.Data.All(p => p.Language == language).Should().BeTrue();
    }

    [Fact]
    public async Task GetPackageDetails_WithLanguageParameter_ShouldReturnCorrectPackage()
    {
        // Arrange
        var expectedResponse = new PackageDetailResponse
        {
            PackageId = "test-package",
            Version = "1.0.0",
            Language = "python",
            Description = "Test Python package",
            Author = "Test Author",
            License = "MIT",
            ExternalDependencies = new List<ExternalDependencyInfo>
            {
                new() { PackageName = "numpy", VersionSpec = ">=1.21.0" }
            }
        };

        _mockSearchService.Setup(s => s.GetPackageDetailsAsync("test-package", "1.0.0", "python"))
                      .ReturnsAsync(expectedResponse);

        // Act
        var result = await _packagesController.GetPackageDetails("test-package", "1.0.0", "python");

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        
        var responseValue = okResult!.Value as PackageDetailResponse;
        responseValue.Should().NotBeNull();
        responseValue!.PackageId.Should().Be("test-package");
        responseValue.Language.Should().Be("python");
        responseValue.ExternalDependencies.Should().HaveCount(1);
    }

    [Fact]
    public async Task UploadPackage_WithPythonLanguage_ShouldCallPackageManagementService()
    {
        // Arrange
        var packageStream = new MemoryStream();
        var packageFile = new Mock<IFormFile>();
        packageFile.Setup(f => f.FileName).Returns("test-package-1.0.0.o8pkg");
        packageFile.Setup(f => f.Length).Returns(1024L);
        packageFile.Setup(f => f.OpenReadStream()).Returns(packageStream);

        var request = new PackageUploadRequest
        {
            PackageFile = packageFile.Object,
            Language = "python",
            Author = "Test Author",
            Description = "Test Python package",
            ExternalDependencies = new List<ExternalDependencyInfo>
            {
                new() { PackageName = "requests", VersionSpec = ">=2.28.0" }
            }
        };

        var expectedPackage = new PackageEntity
        {
            PackageId = "test-package",
            Version = "1.0.0",
            Language = "python",
            Description = "Test Python package",
            Author = "Test Author",
            PackageTags = new List<PackageTagEntity>(),
            PackageDependencies = new List<PackageDependencyEntity>(),
            PublishedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DownloadCount = 0,
            Size = 1024L,
            Checksum = "test-checksum",
            IsListed = true,
            IsPrerelease = false
        };

        _mockPackageService.Setup(s => s.UploadPackageAsync(request, It.IsAny<Stream>()))
                       .ReturnsAsync(expectedPackage);

        // Act
        var result = await _packagesController.UploadPackage(request);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();

        var responseValue = createdResult!.Value as PackageDetailResponse;
        responseValue.Should().NotBeNull();
        responseValue!.PackageId.Should().Be("test-package");
    }

    [Theory]
    [InlineData("invalid-package.txt", 1024L)]
    [InlineData("invalid-package.whl", 1024L)]
    [InlineData("empty-package.o8pkg", 0L)]
    public async Task UploadPackage_WithInvalidFile_ShouldReturnBadRequest(string fileName, long fileSize)
    {
        // Arrange
        var packageFile = new Mock<IFormFile>();
        packageFile.Setup(f => f.FileName).Returns(fileName);
        packageFile.Setup(f => f.Length).Returns(fileSize);

        var request = new PackageUploadRequest
        {
            PackageFile = packageFile.Object,
            Language = "python"
        };

        // Act
        var result = await _packagesController.UploadPackage(request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull("文件 {0} 应该被拒绝", fileName);
    }

    [Theory]
    [InlineData("python", "requests")]
    [InlineData("python", "numpy")]
    [InlineData("old8lang", "mypackage")]
    public async Task CrossLanguageSearch_ShouldFilterCorrectly(string language, string expectedPackage)
    {
        // Arrange
        var searchResponse = new PackageSearchResponse
        {
            TotalHits = 1,
            Data = new List<PackageSearchResult>
            {
                new()
                {
                    PackageId = expectedPackage,
                    Language = language,
                    Description = $"Test {expectedPackage} package"
                }
            }
        };

        _mockSearchService.Setup(s => s.SearchAsync(expectedPackage, language, 0, 20))
                      .ReturnsAsync(searchResponse);

        // Act
        var result = await _packagesController.SearchPackages(expectedPackage, language, 0, 20);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        
        var responseValue = okResult!.Value as PackageSearchResponse;
        responseValue.Should().NotBeNull();
        responseValue!.Data.Should().HaveCount(1);
        responseValue.Data.First().PackageId.Should().Be(expectedPackage);
        responseValue.Data.First().Language.Should().Be(language);
    }
}