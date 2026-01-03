using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Core.Exceptions;
using Old8Lang.PackageManager.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 默认包依赖解析器
/// </summary>
public class DefaultPackageResolver : IPackageResolver
{
    private readonly ILogger<DefaultPackageResolver> _logger;

    /// <summary>
    /// 默认包依赖解析器
    /// </summary>
    /// <param name="logger">日志记录器（可选）</param>
    public DefaultPackageResolver(ILogger<DefaultPackageResolver>? logger = null)
    {
        _logger = logger ?? NullLogger<DefaultPackageResolver>.Instance;
    }

    /// <summary>
    /// 解析依赖
    /// </summary>
    /// <param name="packageId">包ID</param>
    /// <param name="version">包版本</param>
    /// <param name="sources">包源列表</param>
    /// <returns>解析结果</returns>
    public async Task<ResolveResult> ResolveDependenciesAsync(string packageId, string version,
        IEnumerable<IPackageSource> sources)
    {
        _logger.ResolvingDependencies(packageId, version);

        var result = new ResolveResult();
        var resolvedPackages = new HashSet<string>();
        var visited = new HashSet<string>();

        try
        {
            await ResolveDependenciesRecursive(packageId, version, sources, resolvedPackages, visited, result);
            result.Success = true;
            result.Message = "Dependencies resolved successfully.";

            _logger.DependenciesResolved(packageId, result.ResolvedDependencies.Count);
        }
        catch (DependencyResolutionException ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            _logger.LogError(ex, "Dependency resolution failed for package '{PackageId}' version '{Version}'",
                packageId, version);
        }
        catch (PackageNotFoundException ex)
        {
            result.Success = false;
            result.Message = $"Package not found: {ex.Message}";
            _logger.LogError(ex, "Package '{PackageId}' version '{Version}' not found during dependency resolution",
                ex.PackageId, ex.Version);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Dependency resolution failed: {ex.Message}";
            _logger.LogError(ex, "Unexpected error during dependency resolution for package '{PackageId}' version '{Version}'",
                packageId, version);
        }

        return result;
    }

    private async Task ResolveDependenciesRecursive(
        string packageId,
        string version,
        IEnumerable<IPackageSource> sources,
        HashSet<string> resolvedPackages,
        HashSet<string> visited,
        ResolveResult result)
    {
        var packageKey = $"{packageId}@{version}";

        // 避免循环依赖
        if (!visited.Add(packageKey))
        {
            _logger.LogDebug("Circular dependency detected for package '{PackageKey}', skipping", packageKey);
            return;
        }

        _logger.LogDebug("Resolving dependencies for package '{PackageId}' version '{Version}'",
            packageId, version);

        // 从源中获取包信息
        Package? package = null;
        var packageSources = sources as IPackageSource[] ?? sources.ToArray();
        var sourceErrors = new List<string>();

        // 并发查询所有源的包元数据
        var metadataTasks = packageSources.Select(async source =>
        {
            try
            {
                var pkg = await source.GetPackageMetadataAsync(packageId, version);
                if (pkg != null)
                {
                    _logger.LogDebug("Found package '{PackageId}' version '{Version}' in source '{SourceName}'",
                        packageId, version, source.Name);
                }
                return (Source: source.Name, Package: pkg, Error: (string?)null);
            }
            catch (PackageSourceException ex)
            {
                _logger.LogWarning(ex, "Failed to get package metadata from source '{SourceName}'", source.Name);
                return (Source: source.Name, Package: (Package?)null, Error: $"{source.Name}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting package metadata from source '{SourceName}'",
                    source.Name);
                return (Source: source.Name, Package: (Package?)null, Error: $"{source.Name}: {ex.Message}");
            }
        });

        var metadataResults = await Task.WhenAll(metadataTasks);

        // 获取第一个成功的结果
        var successfulResult = metadataResults.FirstOrDefault(r => r.Package != null);
        package = successfulResult.Package;

        // 收集所有错误
        sourceErrors.AddRange(metadataResults.Where(r => r.Error != null).Select(r => r.Error!));

        if (package == null)
        {
            var errorDetails = sourceErrors.Any()
                ? $"Errors from sources: {string.Join(", ", sourceErrors)}"
                : "No sources available or package not found in any source.";

            throw new PackageNotFoundException(packageId, version,
                new Exception($"Package {packageId} version {version} not found in any source. {errorDetails}"));
        }

        // 解析每个依赖
        _logger.LogDebug("Package '{PackageId}' has {DependencyCount} dependencies",
            packageId, package.Dependencies.Count);

        foreach (var dependency in package.Dependencies)
        {
            var depKey = $"{dependency.PackageId}@{dependency.VersionRange}";

            if (!resolvedPackages.Contains(depKey))
            {
                // 解析版本范围到具体版本
                var concreteVersion =
                    await ResolveVersionToConcrete(dependency.PackageId, dependency.VersionRange, packageSources);

                if (string.IsNullOrEmpty(concreteVersion))
                {
                    var conflictMessage =
                        $"Cannot resolve version {dependency.VersionRange} for package {dependency.PackageId}";
                    result.Conflicts.Add(conflictMessage);

                    _logger.VersionConflict(dependency.PackageId, conflictMessage,
                        new DependencyResolutionException(conflictMessage, dependency.PackageId));

                    if (dependency.IsRequired)
                    {
                        throw new DependencyResolutionException(
                            $"Required dependency {dependency.PackageId} version {dependency.VersionRange} cannot be resolved.",
                            dependency.PackageId);
                    }

                    continue;
                }

                _logger.LogDebug("Resolved version '{Version}' for dependency '{PackageId}' (requested: '{VersionRange}')",
                    concreteVersion, dependency.PackageId, dependency.VersionRange);

                // 递归解析子依赖
                await ResolveDependenciesRecursive(dependency.PackageId, concreteVersion, packageSources,
                    resolvedPackages,
                    visited, result);

                result.ResolvedDependencies.Add(new PackageDependency
                {
                    PackageId = dependency.PackageId,
                    VersionRange = concreteVersion,
                    IsRequired = dependency.IsRequired
                });

                resolvedPackages.Add(depKey);
            }
        }
    }

    private async Task<string?> ResolveVersionToConcrete(string packageId, string versionRange,
        IEnumerable<IPackageSource> sources)
    {
        _logger.LogDebug("Resolving version range '{VersionRange}' for package '{PackageId}'",
            versionRange, packageId);

        var sourceList = sources.ToList();

        // 并发查询所有源
        var tasks = sourceList.Select(async source =>
        {
            try
            {
                var versions = await source.GetPackageVersionsAsync(packageId, true);
                var versionList = versions as string[] ?? versions.ToArray();

                if (!versionList.Any())
                {
                    _logger.LogDebug("No versions found for package '{PackageId}' in source '{SourceName}'",
                        packageId, source.Name);
                    return (Source: source.Name, Version: (string?)null);
                }

                var concreteVersion = versionList.FirstOrDefault(v => IsVersionCompatible(versionRange, v));

                if (concreteVersion != null)
                {
                    _logger.LogDebug("Resolved version '{Version}' for package '{PackageId}' from source '{SourceName}'",
                        concreteVersion, packageId, source.Name);
                }

                return (Source: source.Name, Version: concreteVersion);
            }
            catch (PackageSourceException ex)
            {
                _logger.LogWarning(ex, "Failed to get versions for package '{PackageId}' from source '{SourceName}'",
                    packageId, source.Name);
                return (Source: source.Name, Version: (string?)null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting versions for package '{PackageId}' from source '{SourceName}'",
                    packageId, source.Name);
                return (Source: source.Name, Version: (string?)null);
            }
        });

        var results = await Task.WhenAll(tasks);

        // 返回第一个成功解析的版本
        var resolvedVersion = results.FirstOrDefault(r => r.Version != null).Version;

        if (resolvedVersion == null)
        {
            _logger.LogWarning("Could not resolve version range '{VersionRange}' for package '{PackageId}' from any source",
                versionRange, packageId);
        }

        return resolvedVersion;
    }

    /// <summary>
    /// 判断版本是否兼容
    /// </summary>
    /// <param name="requestedVersion"></param>
    /// <param name="availableVersion"></param>
    /// <returns></returns>
    public bool IsVersionCompatible(string requestedVersion, string availableVersion)
    {
        // 简单的版本兼容性检查
        if (requestedVersion == "*") return true;
        if (requestedVersion == availableVersion) return true;

        // 处理预发布版本
        if (requestedVersion.EndsWith(".*"))
        {
            var baseVersion = requestedVersion[..^2];
            return availableVersion.StartsWith(baseVersion);
        }

        // 处理版本范围 (例如: "1.0.0-2.0.0")
        if (requestedVersion.Contains('-'))
        {
            var range = ParseVersionRange(requestedVersion);
            return CompareVersions(availableVersion, range.MinVersion) >= 0 &&
                   CompareVersions(availableVersion, range.MaxVersion) <= 0;
        }

        return false;
    }

    /// <summary>
    /// 解析版本范围
    /// </summary>
    /// <param name="versionRange"></param>
    /// <returns></returns>
    public VersionRange ParseVersionRange(string versionRange)
    {
        var range = new VersionRange();

        if (versionRange == "*")
        {
            return range;
        }

        if (versionRange.Contains('-'))
        {
            var parts = versionRange.Split('-');
            range.MinVersion = parts[0].Trim();
            range.MaxVersion = parts[1].Trim();
        }
        else if (versionRange.EndsWith(".*"))
        {
            range.MinVersion = versionRange[..^2];
            range.MaxVersion = range.MinVersion + ".999";
        }
        else
        {
            range.MinVersion = versionRange;
            range.MaxVersion = versionRange;
        }

        return range;
    }

    private int CompareVersions(string version1, string version2)
    {
        var v1Parts = version1.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
        var v2Parts = version2.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();

        var maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

        for (int i = 0; i < maxLength; i++)
        {
            var v1 = i < v1Parts.Length ? v1Parts[i] : 0;
            var v2 = i < v2Parts.Length ? v2Parts[i] : 0;

            if (v1 != v2)
                return v1.CompareTo(v2);
        }

        return 0;
    }
}