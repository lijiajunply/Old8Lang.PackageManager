using Microsoft.AspNetCore.Mvc;
using Old8Lang.PackageManager.Server.Models;
using Old8Lang.PackageManager.Server.Services;
using Old8Lang.PackageManager.Server.Configuration;

namespace Old8Lang.PackageManager.Server.Controllers;

/// <summary>
/// PyPI 兼容 API 控制器
/// </summary>
[ApiController]
[Route("simple")]
public class PyPIController(
    IPackageManagementService packageService,
    IPackageSearchService searchService,
    ILogger<PyPIController> logger)
    : ControllerBase
{
    /// <summary>
    /// PyPI 简单索引 - 获取所有 Python 包列表
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> GetSimpleIndex()
    {
        try
        {
            var pythonPackages = await packageService.SearchPackagesAsync("", "python", 0, int.MaxValue);
            
            var packageNames = pythonPackages
                .Select(p => p.PackageId)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
            
            Response.ContentType = "text/html; charset=utf-8";
            
            // 生成 HTML 格式的包列表（PyPI 格式）
            var html = GenerateSimpleIndexHtml(packageNames);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取 PyPI 简单索引失败");
            return StatusCode(500);
        }
    }
    
    /// <summary>
    /// 获取特定包的版本列表
    /// </summary>
    /// <param name="packageName">包名</param>
    [HttpGet("{packageName}")]
    public async Task<IActionResult> GetPackageVersions(string packageName)
    {
        try
        {
            var packages = await packageService.GetAllVersionsAsync(packageName);
            var pythonPackages = packages.Where(p => p.Language == "python").ToList();
            
            if (!pythonPackages.Any())
            {
                return NotFound();
            }
            
            Response.ContentType = "text/html; charset=utf-8";
            
            // 生成 HTML 格式的版本列表
            var html = GeneratePackageVersionsHtml(packageName, pythonPackages);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取包版本列表失败: {Package}", packageName);
            return StatusCode(500);
        }
    }
    
    /// <summary>
    /// 下载 Python 包
    /// </summary>
    /// <param name="packageName">包名</param>
    /// <param name="fileName">文件名</param>
    [HttpGet("{packageName}/{fileName}")]
    public async Task<IActionResult> DownloadPackage(string packageName, string fileName)
    {
        try
        {
            // 从文件名解析版本
            var version = ExtractVersionFromFileName(fileName);
            if (string.IsNullOrEmpty(version))
            {
                logger.LogWarning("无法从文件名解析版本: {FileName}", fileName);
                return BadRequest();
            }
            
            var package = await packageService.GetPackageAsync(packageName, version, "python");
            if (package == null || package.Language != "python")
            {
                return NotFound();
            }
            
            // 获取包文件流
            var packageStream = await GetPackageFileStreamAsync(packageName, version);
            if (packageStream == null)
            {
                return NotFound();
            }
            
            // 增加下载计数
            await packageService.IncrementDownloadCountAsync(packageName, version);
            
            return File(packageStream, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "下载 Python 包失败: {Package}/{File}", packageName, fileName);
            return StatusCode(500);
        }
    }
    
    /// <summary>
    /// PyPI JSON API - 获取包信息
    /// </summary>
    /// <param name="packageName">包名</param>
    [HttpGet("pypi/{packageName}/json")]
    public async Task<IActionResult> GetPackageJson(string packageName)
    {
        try
        {
            var packages = await packageService.GetAllVersionsAsync(packageName);
            var pythonPackages = packages.Where(p => p.Language == "python").ToList();
            
            if (!pythonPackages.Any())
            {
                return NotFound(new { detail = "Not found" });
            }
            
            var response = GeneratePyPIJsonResponse(packageName, pythonPackages);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取包 JSON 信息失败: {Package}", packageName);
            return StatusCode(500);
        }
    }
    
    /// <summary>
    /// 搜索 Python 包
    /// </summary>
    /// <param name="q">搜索关键词</param>
    /// <param name="page">页码</param>
    /// <param name="per_page">每页数量</param>
    [HttpGet("search")]
    public async Task<IActionResult> SearchPackages([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int per_page = 20)
    {
        try
        {
            var skip = (page - 1) * per_page;
            var searchResult = await searchService.SearchAsync(q, "python", skip, per_page);
            
            // 筛选 Python 包
            var pythonResults = searchResult.Data
                .Where(r => r.Language == "python")
                .ToList();
            
            var response = new
            {
                info = new
                {
                    q,
                    page,
                    per_page,
                    count = pythonResults.Count,
                    next = pythonResults.Count >= per_page ? 
                        $"/simple/search?q={Uri.EscapeDataString(q)}&page={page + 1}&per_page={per_page}" : null,
                    previous = page > 1 ? 
                        $"/simple/search?q={Uri.EscapeDataString(q)}&page={page - 1}&per_page={per_page}" : null
                },
                results = pythonResults.Select(r => new
                {
                    name = r.PackageId,
                    version = r.Version,
                    description = r.Description,
                    author = r.Author,
                    keywords = string.Join(", ", r.Tags),
                    download_count = r.DownloadCount,
                    releases = new[] { r.Version } // 简化实现
                }).ToArray()
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "搜索 Python 包失败: {Query}", q);
            return StatusCode(500);
        }
    }
    
    private string GenerateSimpleIndexHtml(List<string> packageNames)
    {
        var html = @"<!DOCTYPE html>
<html>
<head>
    <title>Simple Index</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        a { display: block; margin: 5px 0; text-decoration: none; color: #0066cc; }
        a:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <h1>Simple Index</h1>";
        
        foreach (var packageName in packageNames)
        {
            html += $"<a href=\"{packageName}/\">{packageName}/</a>\n";
        }
        
        html += @"</body>
</html>";
        
        return html;
    }
    
    private string GeneratePackageVersionsHtml(string packageName, List<PackageEntity> packages)
    {
        var html = $@"<!DOCTYPE html>
<html>
<head>
    <title>Links for {packageName}</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        a {{ display: block; margin: 5px 0; text-decoration: none; color: #0066cc; }}
        a:hover {{ text-decoration: underline; }}
        .back {{ margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class=""back""><a href=""/simple/"">← Back to simple index</a></div>
    <h1>Links for {packageName}</h1>";
        
        foreach (var package in packages.OrderByDescending(p => p.Version))
        {
            var fileName = $"{packageName}-{package.Version}-py3-none-any.whl"; // 简化的文件名
            var size = FormatFileSize(package.Size);
            html += $"<a href=\"{fileName}\" title=\"Size: {size}, Downloads: {package.DownloadCount}\">{fileName}</a>\n";
        }
        
        html += @"</body>
</html>";
        
        return html;
    }
    
    private object GeneratePyPIJsonResponse(string packageName, List<PackageEntity> packages)
    {
        var releases = new Dictionary<string, object[]>();
        
        foreach (var package in packages)
        {
            var fileName = $"{packageName}-{package.Version}-py3-none-any.whl";
            var size = package.Size;
            var uploadTime = package.PublishedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");
            
            releases[package.Version] = new[]
            {
                new
                {
                    filename = fileName,
                    python_version = "py3",
                    packagetype = "bdist_wheel",
                    url = $"/simple/{packageName}/{fileName}",
                    size = size,
                    upload_time = uploadTime,
                    has_sig = false,
                    md5_digest = package.Checksum,
                    downloads = package.DownloadCount,
                    comment_text = "",
                    yanked = false,
                    yanked_reason = (string?)null
                }
            };
        }
        
        var latestVersion = packages
            .Where(p => !p.IsPrerelease)
            .OrderByDescending(p => p.Version)
            .FirstOrDefault();
        
        return new
        {
            info = new
            {
                name = packageName,
                version = latestVersion?.Version ?? packages.First().Version,
                summary = latestVersion?.Description ?? "",
                description = latestVersion?.Description ?? "",
                author = latestVersion?.Author ?? "",
                license = latestVersion?.License ?? "",
                homepage = latestVersion?.ProjectUrl ?? "",
                keywords = string.Join(", ", latestVersion?.PackageTags.Select(t => t.Tag) ?? new List<string>()),
                classifiers = new string[0], // 需要从语言元数据中提取
                requires_dist = new string[0], // 需要从依赖中提取
                requires_python = ">=3.6", // 默认值
                yanked = false,
                yanked_reason = (string?)null
            },
            last_serial = DateTime.UtcNow.Ticks,
            releases = releases,
            urls = new object[0]
        };
    }
    
    private string ExtractVersionFromFileName(string fileName)
    {
        // 从文件名解析版本：package-version-...ext
        var parts = fileName.Split('-');
        if (parts.Length >= 2)
        {
            var version = parts[1];
            // 移除可能的构建标识
            var versionEndIndex = version.IndexOfAny(new[] { '.', '_' }, 1);
            if (versionEndIndex > 0 && char.IsLetter(version[versionEndIndex]))
            {
                version = version.Substring(0, versionEndIndex);
            }
            return version;
        }
        
        return string.Empty;
    }
    
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
    
    private async Task<Stream?> GetPackageFileStreamAsync(string packageName, string version)
    {
        // 这里应该调用包存储服务获取文件流
        // 为了简化示例，返回 null
        return null;
    }
}