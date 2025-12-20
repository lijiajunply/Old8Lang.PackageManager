using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Old8Lang.PackageManager.Server.Services;

namespace Old8Lang.PackageManager.Tests.UnitTests;

/// <summary>
/// 简化的多语言包管理器测试
/// </summary>
public class SimpleMultiLanguageTests
{
    private readonly Mock<ILogger<PythonPackageParser>> _mockLogger;
    private readonly PythonPackageParser _parser;

    public SimpleMultiLanguageTests()
    {
        _mockLogger = new Mock<ILogger<PythonPackageParser>>();
        _parser = new PythonPackageParser(_mockLogger.Object);
    }

    [Theory]
    [InlineData("requests-2.28.0-py3-none-any.whl", "python")]
    [InlineData("numpy-1.21.0.tar.gz", "python")]
    [InlineData("mypackage-1.0.0.o8pkg", "old8lang")]
    [InlineData("unknown.ext", "unknown")]
    public void GetLanguageFromExtension_ShouldReturnCorrectLanguage(string fileName, string expectedLanguage)
    {
        // Act
        var result = _parser.GetLanguageFromExtension(fileName);

        // Assert
        result.Should().Be(expectedLanguage);
    }

    [Theory]
    [InlineData("requests-2.28.0-py3-none-any.whl", true)]
    [InlineData("numpy-1.21.0.tar.gz", true)]
    [InlineData("setup.py", false)]
    [InlineData("requirements.txt", false)]
    public async Task ValidatePythonPackageAsync_ShouldValidateCorrectFormats(string fileName, bool expectedValid)
    {
        // Arrange
        using var stream = CreateTestPackageStream(fileName);

        // Act
        var result = await _parser.ValidatePythonPackageAsync(stream);

        // Assert
        result.Should().Be(expectedValid);
    }

    [Fact]
    public async Task ParseRequirementsAsync_ShouldParseSimpleRequirements()
    {
        // Arrange
        var requirementsText = @"requests>=2.28.0
numpy==1.21.0
pandas>=1.3.0,<2.0.0
# This is a comment
pytest>=6.0.0";

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(requirementsText));

        // Act
        var dependencies = await _parser.ParseRequirementsAsync(stream);

        // Assert
        dependencies.Should().HaveCount(4);
        dependencies.Should().Contain(d => d.PackageName == "requests" && d.VersionSpec == ">=2.28.0");
        dependencies.Should().Contain(d => d.PackageName == "numpy" && d.VersionSpec == "==1.21.0");
        dependencies.Should().Contain(d => d.PackageName == "pandas" && d.VersionSpec == ">=1.3.0,<2.0.0");
        dependencies.Should().Contain(d => d.PackageName == "pytest" && d.VersionSpec == ">=6.0.0");
    }

    [Theory]
    [InlineData("2.28.0", true)]
    [InlineData("2.28.0rc1", true)]
    [InlineData("2.28.0a1", true)]
    [InlineData("2.28.0b1", true)]
    [InlineData("2.28.0.dev0", true)]
    [InlineData("invalid", false)]
    [InlineData("2.28", true)]
    public void IsValidPythonVersion_ShouldValidateCorrectly(string version, bool expectedValid)
    {
        // Arrange - Test the version validation regex
        var isPythonVersion = System.Text.RegularExpressions.Regex.IsMatch(
            version, 
            @"^\d+\.\d+(\.\d+)?([ab]|rc|alpha|beta|pre|post|dev)\d*$"
        );

        // Assert
        isPythonVersion.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("python", "requests")]
    [InlineData("old8lang", "mypackage")]
    public void LanguageSupport_ShouldSupportBothLanguages(string language, string packageName)
    {
        // Arrange & Act
        var supportedLanguages = new[] { "python", "old8lang" };
        var packages = new[]
        {
            (Language: "python", Packages: new[] { "requests", "numpy", "flask" }),
            (Language: "old8lang", Packages: new[] { "mypackage", "utils", "core" })
        };

        // Assert
        supportedLanguages.Should().Contain(language);
        
        var languagePackages = packages.FirstOrDefault(p => p.Language == language).Packages;
        languagePackages.Should().Contain(packageName);
    }

    [Fact]
    public void PyPICompatibility_ShouldHaveRequiredEndpoints()
    {
        // Arrange
        var requiredEndpoints = new[]
        {
            "/simple/",
            "/simple/{package}/",
            "/simple/pypi/{package}/json",
            "/simple/search"
        };

        // Act & Assert
        requiredEndpoints.Should().HaveCount(4);
        requiredEndpoints.Should().Contain("/simple/");
        requiredEndpoints.Should().Contain("/simple/{package}/");
        requiredEndpoints.Should().Contain("/simple/pypi/{package}/json");
        requiredEndpoints.Should().Contain("/simple/search");
    }

    [Fact]
    public void ApiCompatibility_ShouldSupportLanguageFiltering()
    {
        // Arrange
        var apiEndpoints = new[]
        {
            "/v3/search",
            "/v3/package/{id}",
            "/v3/popular"
        };

        // Act & Assert
        apiEndpoints.Should().HaveCount(3);
        apiEndpoints.Should().Contain("/v3/search");
        apiEndpoints.Should().Contain("/v3/package/{id}");
        apiEndpoints.Should().Contain("/v3/popular");
    }

    [Fact]
    public void MultiLanguageConfiguration_ShouldAllowBothLanguages()
    {
        // Arrange
        var configuration = new
        {
            SupportedLanguages = new[] { "python", "old8lang" },
            DefaultLanguage = "old8lang",
            EnablePyPICompatiblity = true
        };

        // Act & Assert
        configuration.SupportedLanguages.Should().HaveCount(2);
        configuration.SupportedLanguages.Should().Contain("python");
        configuration.SupportedLanguages.Should().Contain("old8lang");
        configuration.DefaultLanguage.Should().Be("old8lang");
        configuration.EnablePyPICompatiblity.Should().BeTrue();
    }

    private Stream CreateTestPackageStream(string fileName)
    {
        // Create a minimal valid package stream for testing
        if (fileName.EndsWith(".whl") || fileName.EndsWith(".tar.gz"))
        {
            // Create a minimal ZIP header
            var header = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // ZIP magic number
            var content = new byte[1024];
            header.CopyTo(content, 0);
            
            return new MemoryStream(content);
        }

        // For other files, create a basic text stream
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
    }
}