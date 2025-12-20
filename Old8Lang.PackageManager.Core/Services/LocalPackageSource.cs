using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;
using System.Text.Json;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 本地包源 - 从本地文件系统加载包
/// </summary>
public class LocalPackageSource : IPackageSource
{
    public string Name { get; private set; }
    public string Source { get; private set; }
    public bool IsEnabled { get; set; } = true;
    
    private readonly Dictionary<string, List<Package>> _packageCache = new();
    
    public LocalPackageSource(string name, string sourcePath)
    {
        Name = name;
        Source = sourcePath;
        InitializeCache();
    }
    
    private void InitializeCache()
    {
        if (!Directory.Exists(Source))
            return;
            
        try
        {
            var metadataFiles = Directory.GetFiles(Source, "*.metadata.json", SearchOption.AllDirectories);
            
            foreach (var metadataFile in metadataFiles)
            {
                var json = File.ReadAllText(metadataFile);
                var package = JsonSerializer.Deserialize<Package>(json);
                if (package != null)
                {
                    if (!_packageCache.ContainsKey(package.Id))
                    {
                        _packageCache[package.Id] = new List<Package>();
                    }
                    _packageCache[package.Id].Add(package);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing local package source cache: {ex.Message}");
        }
    }
    
    public async Task<IEnumerable<Package>> SearchPackagesAsync(string searchTerm, bool includePrerelease = false)
    {
        return await Task.Run(() =>
        {
            var results = new List<Package>();
            
            foreach (var kvp in _packageCache)
            {
                if (kvp.Key.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    var packages = kvp.Value;
                    if (!includePrerelease)
                    {
                        packages = packages.Where(p => !p.Version.Contains('-')).ToList();
                    }
                    results.AddRange(packages);
                }
            }
            
            return results.OrderByDescending(p => p.PublishedAt);
        });
    }
    
    public async Task<IEnumerable<string>> GetPackageVersionsAsync(string packageId, bool includePrerelease = false)
    {
        return await Task.Run(() =>
        {
            if (_packageCache.TryGetValue(packageId, out var packages))
            {
                var versions = packages.Select(p => p.Version);
                if (!includePrerelease)
                {
                    versions = versions.Where(v => !v.Contains('-'));
                }
                return versions.OrderByDescending(v => v);
            }
            return Enumerable.Empty<string>();
        });
    }
    
    public async Task<Stream> DownloadPackageAsync(string packageId, string version)
    {
        if (_packageCache.TryGetValue(packageId, out var packages))
        {
            var package = packages.FirstOrDefault(p => p.Version == version);
            if (package != null && File.Exists(package.FilePath))
            {
                return await Task.FromResult(File.OpenRead(package.FilePath));
            }
        }
        
        throw new FileNotFoundException($"Package {packageId} version {version} not found in local source.");
    }
    
    public async Task<Package?> GetPackageMetadataAsync(string packageId, string version)
    {
        return await Task.Run(() =>
        {
            if (_packageCache.TryGetValue(packageId, out var packages))
            {
                return packages.FirstOrDefault(p => p.Version == version);
            }
            return null;
        });
    }
}