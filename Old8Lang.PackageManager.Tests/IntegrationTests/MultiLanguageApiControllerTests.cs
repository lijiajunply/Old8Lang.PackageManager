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

namespace Old8Lang.PackageManager.Tests.IntegrationTests;

/// <summary>
/// 多语言 API 控制器集成测试
/// </summary>
public class MultiLanguageApiControllerTests
{
    private readonly Mock<IPackageManagementService> _mockPackageService;
    private readonly Mock<IPackageSearchService> _mockSearchService;
    private readonly Mock<ILogger<PackagesController>> _mockLogger;
    private readonly PackagesController _packagesController;

    public MultiLanguageApiControllerTests()
    {
        _mockPackageService = new Mock<IPackageManagementService>();
        _mockSearchService = new Mock<IPackageSearchService>();
        _mockLogger = new Mock<ILogger<PackagesController>>();

        _packagesController = new PackagesController(
            _mockSearchService.Object,
            _mockPackageService.Object,
            new Mock<IPackageQualityService>().Object,
            new Mock<IPackageDependencyService>().Object,
            new Mock<IApiKeyService>().Object,
            new Mock<ApiOptions>().Object,
            new Mock<ILocalizationService>().Object,
            _mockLogger.Object);
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
        var packageFile = new Mock<IFormFile>();
        packageFile.Setup(f => f.FileName).Returns("test-package-1.0.0-py3-none-any.whl");
        packageFile.Setup(f => f.Length).Returns(1024L);

        var packageStream = new MemoryStream();
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
            Language = "python"
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
        responseValue.Language.Should().Be("python");
    }

    [Theory]
    [InlineData("invalid-package.txt")]
    [InlineData("too-large-package.whl")]
    public async Task UploadPackage_WithInvalidFile_ShouldReturnBadRequest(string fileName)
    {
        // Arrange
        var packageFile = new Mock<IFormFile>();
        packageFile.Setup(f => f.FileName).Returns(fileName);
        
        if (fileName == "too-large-package.whl")
        {
            packageFile.Setup(f => f.Length).Returns(200 * 1024 * 1024L); // 200MB
        }
        else
        {
            packageFile.Setup(f => f.Length).Returns(1024L);
        }

        var request = new PackageUploadRequest
        {
            PackageFile = packageFile.Object,
            Language = "python"
        };

        // Act
        var result = await _packagesController.UploadPackage(request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
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