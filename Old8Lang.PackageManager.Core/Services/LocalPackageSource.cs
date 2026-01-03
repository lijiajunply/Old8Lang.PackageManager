using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 本地包源 - 从本地文件系统加载包
/// </summary>
public class LocalPackageSource : IPackageSource
{
    private readonly ILogger<LocalPackageSource> _logger;
    private readonly Dictionary<string, List<Package>> _packageCache = new();

    /// <summary>
    /// 包源名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 包源路径
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 本地包源
    /// </summary>
    /// <param name="name">包源名称</param>
    /// <param name="sourcePath">本地路径</param>
    /// <param name="logger">日志记录器（可选）</param>
    public LocalPackageSource(string name, string sourcePath, ILogger<LocalPackageSource>? logger = null)
    {
        Name = name;
        Source = sourcePath;
        _logger = logger ?? NullLogger<LocalPackageSource>.Instance;
        InitializeCache();
    }

    private void InitializeCache()
    {
        if (!Directory.Exists(Source))
        {
            _logger.LogWarning("Local package source directory '{SourcePath}' does not exist for source '{SourceName}'",
                Source, Name);
            return;
        }

        try
        {
            _logger.LogInformation("Initializing local package source '{SourceName}' from path '{SourcePath}'",
                Name, Source);

            var metadataFiles = Directory.GetFiles(Source, "*.metadata.json", SearchOption.AllDirectories);
            _logger.LogDebug("Found {Count} metadata files in source '{SourceName}'", metadataFiles.Length, Name);

            var loadedCount = 0;
            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var json = File.ReadAllText(metadataFile);
                    var package = JsonSerializer.Deserialize<Package>(json);
                    if (package != null)
                    {
                        if (!_packageCache.TryGetValue(package.Id, out var value))
                        {
                            value = [];
                            _packageCache[package.Id] = value;
                        }

                        value.Add(package);
                        loadedCount++;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize package metadata from file '{FilePath}'", metadataFile);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse package metadata from file '{FilePath}'", metadataFile);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "IO error while reading metadata file '{FilePath}'", metadataFile);
                }
            }

            _logger.LogInformation("Successfully loaded {LoadedCount} packages from {TotalPackages} unique IDs in source '{SourceName}'",
                loadedCount, _packageCache.Count, Name);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while initializing local package source '{SourceName}' at '{SourcePath}'",
                Name, Source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error initializing local package source '{SourceName}'", Name);
        }
    }

    /// <summary>
    /// 搜索包
    /// </summary>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="includePrerelease">是否包含预发布版本</param>
    /// <returns>匹配的包列表</returns>
    public async Task<IEnumerable<Package>> SearchPackagesAsync(string searchTerm, bool includePrerelease = false)
    {
        _logger.LogDebug("Searching for packages with term '{SearchTerm}' in local source '{SourceName}'",
            searchTerm, Name);

        return await Task.Run(() =>
        {
            try
            {
                var results = new List<Package>();

                foreach (var kvp in _packageCache)
                {
                    if (!kvp.Key.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) continue;
                    var packages = kvp.Value;
                    if (!includePrerelease)
                    {
                        packages = packages.Where(p => !p.Version.Contains('-')).ToList();
                    }

                    results.AddRange(packages);
                }

                _logger.LogDebug("Found {Count} packages matching term '{SearchTerm}' in local source '{SourceName}'",
                    results.Count, searchTerm, Name);
                return results.OrderByDescending(p => p.PublishedAt).AsEnumerable();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching packages in local source '{SourceName}'", Name);
                return Enumerable.Empty<Package>();
            }
        });
    }

    /// <summary>
    /// 获取包版本
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <param name="includePrerelease">是否包含预发布版本</param>
    /// <returns>版本列表</returns>
    public async Task<IEnumerable<string>> GetPackageVersionsAsync(string packageId, bool includePrerelease = false)
    {
        _logger.LogDebug("Fetching versions for package '{PackageId}' in local source '{SourceName}'",
            packageId, Name);

        return await Task.Run(() =>
        {
            try
            {
                if (_packageCache.TryGetValue(packageId, out var packages))
                {
                    var versions = packages.Select(p => p.Version);
                    if (!includePrerelease)
                    {
                        versions = versions.Where(v => !v.Contains('-'));
                    }

                    var versionList = versions.OrderByDescending(v => v).ToList();
                    _logger.LogDebug("Found {Count} versions for package '{PackageId}' in local source '{SourceName}'",
                        versionList.Count, packageId, Name);
                    return versionList.AsEnumerable();
                }

                _logger.LogDebug("Package '{PackageId}' not found in local source '{SourceName}'",
                    packageId, Name);
                return Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting package versions for '{PackageId}' in local source '{SourceName}'",
                    packageId, Name);
                return Enumerable.Empty<string>();
            }
        });
    }

    /// <summary>
    /// 下载包
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <param name="version">包版本</param>
    /// <returns>包文件流</returns>
    /// <exception cref="PackageNotFoundException">包不存在时抛出</exception>
    public async Task<Stream> DownloadPackageAsync(string packageId, string version)
    {
        _logger.LogDebug("Downloading package '{PackageId}' version '{Version}' from local source '{SourceName}'",
            packageId, version, Name);

        try
        {
            if (_packageCache.TryGetValue(packageId, out var packages))
            {
                var package = packages.FirstOrDefault(p => p.Version == version);
                if (package != null)
                {
                    if (!File.Exists(package.FilePath))
                    {
                        var ex = new PackageNotFoundException(packageId, version,
                            new FileNotFoundException($"Package file not found at path: {package.FilePath}"));
                        _logger.LogError(ex, "Package file '{FilePath}' not found for package '{PackageId}' version '{Version}'",
                            package.FilePath, packageId, version);
                        throw ex;
                    }

                    _logger.LogDebug("Successfully opened package file for '{PackageId}' version '{Version}' at '{FilePath}'",
                        packageId, version, package.FilePath);
                    return await Task.FromResult(File.OpenRead(package.FilePath));
                }
            }

            var notFoundEx = new PackageNotFoundException(packageId, version);
            _logger.LogWarning(notFoundEx, "Package '{PackageId}' version '{Version}' not found in local source '{SourceName}'",
                packageId, version, Name);
            throw notFoundEx;
        }
        catch (PackageNotFoundException)
        {
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while opening package file for '{PackageId}' version '{Version}'",
                packageId, version);
            throw new PackageSourceException(
                $"Failed to open package file for '{packageId}' version '{version}': {ex.Message}",
                ex, Name, Source);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while opening package file for '{PackageId}' version '{Version}'",
                packageId, version);
            throw new PackageSourceException(
                $"Access denied to package file for '{packageId}' version '{version}'",
                ex, Name, Source);
        }
    }

    /// <summary>
    /// 获取包元数据
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <param name="version">包版本</param>
    /// <returns>包元数据</returns>
    public async Task<Package?> GetPackageMetadataAsync(string packageId, string version)
    {
        _logger.LogDebug("Fetching metadata for package '{PackageId}' version '{Version}' from local source '{SourceName}'",
            packageId, version, Name);

        return await Task.Run(() =>
        {
            try
            {
                if (_packageCache.TryGetValue(packageId, out var packages))
                {
                    var package = packages.FirstOrDefault(p => p.Version == version);
                    if (package != null)
                    {
                        _logger.LogDebug("Successfully fetched metadata for package '{PackageId}' version '{Version}'",
                            packageId, version);
                    }
                    else
                    {
                        _logger.LogDebug("Package '{PackageId}' version '{Version}' not found in local source '{SourceName}'",
                            packageId, version, Name);
                    }
                    return package;
                }

                _logger.LogDebug("Package '{PackageId}' not found in local source '{SourceName}'",
                    packageId, Name);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metadata for package '{PackageId}' version '{Version}' in local source '{SourceName}'",
                    packageId, version, Name);
                return null;
            }
        });
    }
}