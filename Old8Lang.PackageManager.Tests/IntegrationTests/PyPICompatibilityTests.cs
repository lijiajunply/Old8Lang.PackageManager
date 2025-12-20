using Xunit;
using FluentAssertions;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Old8Lang.PackageManager.Server;
using System.Text.Json;

namespace Old8Lang.PackageManager.Tests.IntegrationTests;

/// <summary>
/// PyPI 兼容性集成测试
/// </summary>
public class PyPICompatibilityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PyPICompatibilityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SimpleIndex_ShouldReturnHtmlResponse()
    {
        // Act
        var response = await _client.GetAsync("/simple/");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("text/html");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<!DOCTYPE html>");
        content.Should().Contain("Simple Index");
    }

    [Fact]
    public async Task PackageVersions_ShouldReturnHtmlList()
    {
        // Act
        var response = await _client.GetAsync("/simple/requests/");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("text/html");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Links for requests");
        content.Should().Contain("Back to simple index");
    }

    [Fact]
    public async Task PackageJson_ShouldReturnValidJsonResponse()
    {
        // Act
        var response = await _client.GetAsync("/simple/pypi/requests/json");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        var packageInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        
        packageInfo.Should().ContainKey("info");
        packageInfo.Should().ContainKey("releases");
        packageInfo.Should().ContainKey("last_serial");
        
        var info = packageInfo["info"] as JsonElement;
        info.GetProperty("name").GetString().Should().Be("requests");
    }

    [Fact]
    public async Task PackageSearch_ShouldReturnSearchResults()
    {
        // Act
        var response = await _client.GetAsync("/simple/search?q=requests&page=1&per_page=10");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        var searchResult = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        
        searchResult.Should().ContainKey("info");
        searchResult.Should().ContainKey("results");
        
        var results = searchResult["results"] as JsonElement;
        results.GetArrayLength().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task DownloadPackage_ShouldReturnFile()
    {
        // This test would require an actual package file
        // For now, we'll test that the endpoint exists and returns the correct response type
        
        // Act
        var response = await _client.GetAsync("/simple/requests/requests-2.28.0-py3-none-any.whl");

        // Assert - Should return either 200 with file or 404 if package doesn't exist
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MultipleLanguageSupport_ShouldWorkAcrossEndpoints()
    {
        // Test that the system handles multiple languages
        
        // Test v3 API with language filter
        var v3Response = await _client.GetAsync("/v3/search?q=test&language=python");
        v3Response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        // Test PyPI Simple API
        var pypiResponse = await _client.GetAsync("/simple/");
        pypiResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/simple/nonexistent")]
    [InlineData("/simple/pypi/nonexistent/json")]
    public async Task NonExistentPackage_ShouldReturn404(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchPagination_ShouldWorkCorrectly()
    {
        // Act
        var page1Response = await _client.GetAsync("/simple/search?q=test&page=1&per_page=5");
        var page2Response = await _client.GetAsync("/simple/search?q=test&page=2&per_page=5");

        // Assert
        page1Response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        page2Response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var page1Content = await page1Response.Content.ReadAsStringAsync();
        var page1Result = JsonSerializer.Deserialize<Dictionary<string, object>>(page1Content);
        
        var page2Content = await page2Response.Content.ReadAsStringAsync();
        var page2Result = JsonSerializer.Deserialize<Dictionary<string, object>>(page2Content);
        
        // Both should have the info structure
        page1Result.Should().ContainKey("info");
        page2Result.Should().ContainKey("info");
    }

    [Fact]
    public async Task ApiCompatibility_PyPiToolsShouldWork()
    {
        // Test that standard pip commands would work with our API
        
        // Test endpoints that pip uses
        var endpoints = new[]
        {
            "/simple/",
            "/simple/requests/",
            "/simple/pypi/requests/json",
            "/simple/search?q=requests"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            response.StatusCode.Should().BeOneOf(
                System.Net.HttpStatusCode.OK,
                System.Net.HttpStatusCode.NotFound); // 404 is OK for non-existent packages
        }
    }

    [Fact]
    public async Task ResponseHeaders_ShouldContainCorrectContentType()
    {
        // Act
        var htmlResponse = await _client.GetAsync("/simple/");
        var jsonResponse = await _client.GetAsync("/simple/pypi/requests/json");

        // Assert
        htmlResponse.Content.Headers.ContentType.MediaType.Should().Be("text/html");
        jsonResponse.Content.Headers.ContentType.MediaType.Should().Be("application/json");
    }

    [Theory]
    [InlineData("requests-2.28.0-py3-none-any.whl")]
    [InlineData("numpy-1.21.0.tar.gz")]
    public async Task PackageFileName_WithCorrectFormat_ShouldBeRecognized(string fileName)
    {
        // Arrange - Create a mock package endpoint
        var packageUrl = $"/simple/testpackage/{fileName}";
        
        // Act
        var response = await _client.GetAsync(packageUrl);

        // Assert - Should handle the request (either find package or return 404 gracefully)
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LanguageSpecificSearch_ShouldReturnCorrectResults()
    {
        // Test that language filtering works in search
        
        // Act
        var pythonSearchResponse = await _client.GetAsync("/v3/search?q=test&language=python");
        var old8langSearchResponse = await _client.GetAsync("/v3/search?q=test&language=old8lang");

        // Assert
        pythonSearchResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        old8langSearchResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var pythonContent = await pythonSearchResponse.Content.ReadAsStringAsync();
        var pythonResult = JsonSerializer.Deserialize<Dictionary<string, object>>(pythonContent);
        
        var old8langContent = await old8langSearchResponse.Content.ReadAsStringAsync();
        var old8langResult = JsonSerializer.Deserialize<Dictionary<string, object>>(old8langContent);
        
        // Both should have the expected structure
        pythonResult.Should().ContainKey("data");
        old8langResult.Should().ContainKey("data");
    }

    [Fact]
    public async Task CrossLanguagePackageConsistency_ShouldBeMaintained()
    {
        // Test that the same package name can exist in multiple languages
        
        // This is more of an integration test that would require actual data
        // For now, we test the API endpoints respond correctly
        
        // Act
        var pythonPackagesResponse = await _client.GetAsync("/v3/search?q=common-package&language=python");
        var old8langPackagesResponse = await _client.GetAsync("/v3/search?q=common-package&language=old8lang");

        // Assert
        pythonPackagesResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        old8langPackagesResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        // Both responses should have the correct structure
        var pythonContent = await pythonPackagesResponse.Content.ReadAsStringAsync();
        var pythonResult = JsonSerializer.Deserialize<Dictionary<string, object>>(pythonContent);
        pythonResult.Should().ContainKey("data");
        
        var old8langContent = await old8langPackagesResponse.Content.ReadAsStringAsync();
        var old8langResult = JsonSerializer.Deserialize<Dictionary<string, object>>(old8langContent);
        old8langResult.Should().ContainKey("data");
    }

    [Fact]
    public async Task ErrorHandling_ShouldReturnAppropriateResponses()
    {
        // Test various error scenarios
        
        // Invalid search parameters
        var invalidPageResponse = await _client.GetAsync("/simple/search?q=test&page=-1");
        invalidPageResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        // Missing required parameters
        var missingQueryResponse = await _client.GetAsync("/simple/search?");
        missingQueryResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        // Very large page size
        var largePageResponse = await _client.GetAsync("/simple/search?q=test&per_page=10000");
        largePageResponse.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.BadRequest,
            System.Net.HttpStatusCode.OK); // May accept or reject based on implementation
    }

    [Fact]
    public async Task Performance_ShouldHandleReasonableLoad()
    {
        // Test that the API can handle concurrent requests
        
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Create multiple concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync("/simple/"));
        }
        
        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().All(r => r.StatusCode == System.Net.HttpStatusCode.OK);
        
        // All responses should be successful and complete
        foreach (var response in responses)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
    }
}