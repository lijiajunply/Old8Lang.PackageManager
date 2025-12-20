using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 默认包依赖解析器
/// </summary>
public class DefaultPackageResolver : IPackageResolver
{
    public async Task<ResolveResult> ResolveDependenciesAsync(string packageId, string version, IEnumerable<IPackageSource> sources)
    {
        var result = new ResolveResult();
        var resolvedPackages = new HashSet<string>();
        var visited = new HashSet<string>();
        
        try
        {
            await ResolveDependenciesRecursive(packageId, version, sources, resolvedPackages, visited, result);
            result.Success = true;
            result.Message = "Dependencies resolved successfully.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Dependency resolution failed: {ex.Message}";
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
        if (visited.Contains(packageKey))
            return;
        
        visited.Add(packageKey);
        
        // 从源中获取包信息
        Package? package = null;
        foreach (var source in sources)
        {
            try
            {
                package = await source.GetPackageMetadataAsync(packageId, version);
                if (package != null) break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting package metadata from {source.Name}: {ex.Message}");
            }
        }
        
        if (package == null)
        {
            throw new InvalidOperationException($"Package {packageId} version {version} not found in any source.");
        }
        
        // 解析每个依赖
        foreach (var dependency in package.Dependencies)
        {
            var depKey = $"{dependency.PackageId}@{dependency.VersionRange}";
            
            if (!resolvedPackages.Contains(depKey))
            {
                // 解析版本范围到具体版本
                var concreteVersion = await ResolveVersionToConcrete(dependency.PackageId, dependency.VersionRange, sources);
                if (string.IsNullOrEmpty(concreteVersion))
                {
                    result.Conflicts.Add($"Cannot resolve version {dependency.VersionRange} for package {dependency.PackageId}");
                    if (dependency.IsRequired)
                    {
                        throw new InvalidOperationException($"Required dependency {dependency.PackageId} version {dependency.VersionRange} cannot be resolved.");
                    }
                    continue;
                }
                
                // 递归解析子依赖
                await ResolveDependenciesRecursive(dependency.PackageId, concreteVersion, sources, resolvedPackages, visited, result);
                
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
    
    private async Task<string?> ResolveVersionToConcrete(string packageId, string versionRange, IEnumerable<IPackageSource> sources)
    {
        foreach (var source in sources)
        {
            try
            {
                var versions = await source.GetPackageVersionsAsync(packageId, true);
                var concreteVersion = versions.FirstOrDefault(v => IsVersionCompatible(versionRange, v));
                
                if (concreteVersion != null)
                    return concreteVersion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting versions for {packageId} from {source.Name}: {ex.Message}");
            }
        }
        
        return null;
    }
    
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