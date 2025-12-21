using Microsoft.AspNetCore.Mvc;
using Old8Lang.PackageManager.Server.Models;
using Old8Lang.PackageManager.Server.Services;
using System.Text.Json;

namespace Old8Lang.PackageManager.Server.Controllers;

/// <summary>
/// NPM 兼容 API 控制器
/// </summary>
[ApiController]
[Route("npm")]
public class NpmController(
    IPackageManagementService packageService,
    IPackageStorageService storageService,
    IJavaScriptPackageParser jsParser,
    ILogger<NpmController> logger)
    : ControllerBase
{
    /// <summary>
    /// NPM 注册表根端点
    /// </summary>
    [HttpGet("")]
    public IActionResult GetRegistryInfo()
    {
        try
        {
            var registryInfo = new
            {
                name = "old8lang-npm-registry",
                version = "1.0.0",
                description = "Old8Lang Package Manager NPM Compatible Registry",
                maintainers = new[] { new { name = "Old8Lang Team", email = "team@old8lang.org" } },
                readme = "Welcome to Old8Lang NPM Registry"
            };

            return Ok(registryInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取注册表信息失败");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// 获取包信息 (NPM 兼容)
    /// </summary>
    [HttpGet("{*packageName}")]
    public async Task<IActionResult> GetPackageInfo(string packageName)
    {
        try
        {
            if (string.IsNullOrEmpty(packageName))
            {
                return BadRequest(new { error = "Package name is required" });
            }

            // 解码包名
            packageName = Uri.UnescapeDataString(packageName);

            // 从路径中提取包名 (可能是 scope 包)
            var parts = packageName.Split('/');
            if (parts.Length >= 2 && parts[0].StartsWith("@"))
            {
                packageName = string.Join("/", parts);
            }
            else
            {
                packageName = parts[^1];
            }

            // 查询包信息 - 获取最新版本
            var packages = await packageService.GetAllVersionsAsync(packageName);
            var package = packages.FirstOrDefault(p => p.Language == "javascript");
            if (package == null)
            {
                return NotFound(new { error = "Package not found" });
            }

            // 获取包的标签和依赖
            package.Tags = package.PackageTags.Select(t => t.Tag).ToList();

            // 转换为 NPM 格式响应
            var npmResponse = new
            {
                name = package.PackageId,
                description = package.Description,
                versions = new Dictionary<string, object>
                {
                    [package.Version] = new
                    {
                        name = package.PackageId,
                        version = package.Version,
                        description = package.Description,
                        author = package.Author,
                        license = package.License,
                        homepage = package.ProjectUrl,
                        main = GetLanguageMetadataValue(package, "main", "index.js"),
                        types = GetLanguageMetadataValue(package, "types", ""),
                        module = GetLanguageMetadataValue(package, "module", ""),
                        files = GetLanguageMetadataValue(package, "files", "")
                            .Split(',', StringSplitOptions.RemoveEmptyEntries),
                        engines = ParseEngines(GetLanguageMetadataValue(package, "engines", "")),
                        dependencies =
                            ParseExternalDependencies(package.ExternalDependencies.Where(d => !d.IsDevDependency)),
                        devDependencies =
                            ParseExternalDependencies(package.ExternalDependencies.Where(d => d.IsDevDependency)),
                        dist = new
                        {
                            tarball =
                                $"{Request.Scheme}://{Request.Host}/npm/download/{package.PackageId}/-/{package.PackageId}-{package.Version}.tgz",
                            shasum = package.Checksum,
                            integrity = $"sha512-{package.Checksum}"
                        }
                    }
                },
                time = new Dictionary<string, string>
                {
                    [package.Version] = package.PublishedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ["created"] = package.PublishedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ["modified"] = package.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                },
                homepage = package.ProjectUrl,
                keywords = package.Tags,
                repository = new
                {
                    type = "git",
                    url = package.ProjectUrl
                },
                license = package.License,
                readme = package.Description,
                distTags = new { latest = package.Version },
                downloads = new { lastWeek = package.DownloadCount }
            };

            return Ok(npmResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取包信息失败: {PackageName}", packageName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// 下载包文件 (NPM 兼容)
    /// </summary>
    [HttpGet("download/{*packageName}/{fileName}")]
    public async Task<IActionResult> DownloadPackage(string packageName, string fileName)
    {
        try
        {
            if (string.IsNullOrEmpty(packageName) || string.IsNullOrEmpty(fileName))
            {
                return BadRequest(new { error = "Package name and file name are required" });
            }

            // 从文件名解析版本
            var version = ExtractVersionFromFileName(fileName);
            if (string.IsNullOrEmpty(version))
            {
                return BadRequest(new { error = "Invalid file name format" });
            }

            var package = await packageService.GetPackageAsync(packageName, version, "javascript");
            if (package == null)
            {
                return NotFound(new { error = "Package not found" });
            }

            var packageStream = await storageService.GetPackageAsync(package.PackageId, package.Version);
            if (packageStream == null)
            {
                return NotFound(new { error = "Package file not found" });
            }

            return File(packageStream, "application/gzip", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "下载包失败: {PackageName}/{FileName}", packageName, fileName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// 搜索包 (NPM 兼容)
    /// </summary>
    [HttpGet("-/v1/search")]
    public async Task<IActionResult> SearchPackages([FromQuery] string text = "", [FromQuery] int from = 0,
        [FromQuery] int size = 20, [FromQuery] string quality = "0.65", [FromQuery] string popularity = "0.98",
        [FromQuery] string maintenance = "0.5")
    {
        try
        {
            var searchResult = await packageService.SearchPackagesAsync(text, "javascript", from, size);

            var npmSearchResponse = new
            {
                objects = searchResult.Select(pkg => new
                {
                    package = new
                    {
                        name = pkg.PackageId,
                        scope = GetScope(pkg.PackageId),
                        version = pkg.Version,
                        description = pkg.Description,
                        keywords = pkg.PackageTags.Select(t => t.Tag).ToList(),
                        date = pkg.PublishedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        links = new
                        {
                            npm = $"https://www.npmjs.com/package/{pkg.PackageId}",
                            homepage = pkg.ProjectUrl,
                            repository = pkg.ProjectUrl,
                            bugs = ""
                        },
                        publisher = new
                        {
                            username = "old8lang",
                            email = "team@old8lang.org"
                        },
                        maintainers = new[]
                        {
                            new { username = "old8lang", email = "team@old8lang.org" }
                        }
                    },
                    flags = new
                    {
                        unstable = pkg.IsPrerelease
                    },
                    score = new
                    {
                        final = Math.Round(new Random().NextDouble(), 2), // 简化评分
                        detail = new
                        {
                            quality = double.Parse(quality),
                            popularity = double.Parse(popularity),
                            maintenance = double.Parse(maintenance)
                        }
                    },
                    searchScore = Math.Round(new Random().NextDouble(), 2)
                }),
                total = searchResult.Count,
                time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            return Ok(npmSearchResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "搜索包失败: {Text}", text);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// 发布包 (NPM 兼容)
    /// </summary>
    [HttpPut("{*packageName}")]
    public async Task<IActionResult> PublishPackage(string packageName, [FromBody] PublishRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(packageName))
            {
                return BadRequest(new { error = "Package name is required" });
            }

            // 解码包名
            packageName = Uri.UnescapeDataString(packageName);

            // 验证权限 (简化实现)
            if (!await ValidatePublishPermissionAsync(packageName))
            {
                return Unauthorized(new { error = "Unauthorized to publish this package" });
            }

            // 这里应该从请求中提取 tarball 文件并解析
            // 简化实现，返回成功响应
            return Ok(new { success = true, id = $"{packageName}@{request?.Version ?? "1.0.0"}" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发布包失败: {PackageName}", packageName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// 解析 package.json 文件
    /// </summary>
    [HttpPost("parse-package-json")]
    public async Task<IActionResult> ParsePackageJson(IFormFile packageJsonFile)
    {
        try
        {
            if (packageJsonFile == null || packageJsonFile.Length == 0)
            {
                return BadRequest(new { error = "package.json file is required" });
            }

            using var stream = packageJsonFile.OpenReadStream();
            var dependencies = await jsParser.ParsePackageJsonAsync(stream);

            return Ok(new ApiResponse<List<ExternalDependencyInfo>>
            {
                Success = true,
                Message = "package.json parsed successfully",
                Data = dependencies
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "解析 package.json 失败");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to parse package.json",
                ErrorCode = "PARSE_ERROR"
            });
        }
    }

    /// <summary>
    /// 验证 JS/TS 包
    /// </summary>
    [HttpPost("validate-package")]
    public async Task<IActionResult> ValidateJavaScriptPackage(IFormFile packageFile)
    {
        try
        {
            if (packageFile == null || packageFile.Length == 0)
            {
                return BadRequest(new { error = "Package file is required" });
            }

            using var stream = packageFile.OpenReadStream();
            var isValid = await jsParser.ValidateJavaScriptPackageAsync(stream);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Package validation completed",
                Data = isValid
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "验证包失败: {FileName}", packageFile?.FileName);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to validate package",
                ErrorCode = "VALIDATION_ERROR"
            });
        }
    }

    /// <summary>
    /// 删除包版本 (NPM 兼容)
    /// </summary>
    [HttpDelete("{*packageName}/{version}")]
    public async Task<IActionResult> UnpublishPackageVersion(string packageName, string version)
    {
        try
        {
            if (string.IsNullOrEmpty(packageName) || string.IsNullOrEmpty(version))
            {
                return BadRequest(new { error = "Package name and version are required" });
            }

            // 解码包名
            packageName = Uri.UnescapeDataString(packageName);

            // 验证权限
            if (!await ValidatePublishPermissionAsync(packageName))
            {
                return Unauthorized(new { error = "Unauthorized to unpublish this package" });
            }

            // 删除包版本
            var success = await packageService.DeletePackageAsync(packageName, version);
            if (success)
            {
                return Ok(new { success = true });
            }

            return NotFound(new { error = "Package version not found" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除包版本失败: {PackageName}@{Version}", packageName, version);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private string GetLanguageMetadataValue(PackageEntity package, string key, string defaultValue = "")
    {
        var metadata = package.LanguageMetadata.FirstOrDefault(m => m.Language == "javascript");
        if (metadata != null)
        {
            try
            {
                var metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(metadata.Metadata);
                if (metadataDict?.ContainsKey(key) == true)
                {
                    return metadataDict[key].ToString() ?? defaultValue;
                }
            }
            catch
            {
                // 忽略解析错误
            }
        }

        return defaultValue;
    }

    private Dictionary<string, string> ParseEngines(string enginesJson)
    {
        try
        {
            if (string.IsNullOrEmpty(enginesJson))
                return new Dictionary<string, string>();

            var engines = JsonSerializer.Deserialize<Dictionary<string, string>>(enginesJson);
            return engines ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private Dictionary<string, string> ParseExternalDependencies(IEnumerable<ExternalDependencyEntity> dependencies)
    {
        return dependencies.ToDictionary(d => d.PackageName, d => d.VersionSpec);
    }

    private string GetScope(string packageName)
    {
        if (packageName.StartsWith("@"))
        {
            var parts = packageName.Split('/');
            if (parts.Length >= 2)
            {
                return parts[0];
            }
        }

        return null;
    }

    private string ExtractVersionFromFileName(string fileName)
    {
        // 从 {package}-{version}.tgz 格式中提取版本
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"-(\d+\.\d+\.\d+.*)\.tgz$");
        return match.Success ? match.Groups[1].Value : "";
    }

    private Task<bool> ValidatePublishPermissionAsync(string packageName)
    {
        // 简化实现：实际应该验证用户权限
        return Task.FromResult(true);
    }
}

/// <summary>
/// NPM 发布请求
/// </summary>
public class PublishRequest
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Dependencies { get; set; } = new();
    public Dictionary<string, string> DevDependencies { get; set; } = new();
}