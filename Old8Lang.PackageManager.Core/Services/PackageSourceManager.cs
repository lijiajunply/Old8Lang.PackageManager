using Old8Lang.PackageManager.Core.Interfaces;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 包源管理器 - 管理多个包源
/// </summary>
public class PackageSourceManager
{
    private readonly List<IPackageSource> _sources = [];
    
    /// <summary>
    /// 添加包源
    /// </summary>
    public void AddSource(IPackageSource source)
    {
        if (_sources.Any(s => s.Name.Equals(source.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Package source '{source.Name}' already exists.");
        }
        
        _sources.Add(source);
    }
    
    /// <summary>
    /// 移除包源
    /// </summary>
    public bool RemoveSource(string sourceName)
    {
        var source = _sources.FirstOrDefault(s => s.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
        if (source != null)
        {
            return _sources.Remove(source);
        }
        return false;
    }
    
    /// <summary>
    /// 获取所有包源
    /// </summary>
    public IEnumerable<IPackageSource> GetAllSources()
    {
        return _sources.ToList();
    }
    
    /// <summary>
    /// 获取启用的包源
    /// </summary>
    public IEnumerable<IPackageSource> GetEnabledSources()
    {
        return _sources.Where(s => s.IsEnabled).ToList();
    }
    
    /// <summary>
    /// 根据名称获取包源
    /// </summary>
    public IPackageSource? GetSource(string sourceName)
    {
        return _sources.FirstOrDefault(s => s.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// 启用/禁用包源
    /// </summary>
    public bool SetSourceEnabled(string sourceName, bool enabled)
    {
        var source = GetSource(sourceName);
        if (source != null)
        {
            source.IsEnabled = enabled;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 在所有启用的包源中搜索包
    /// </summary>
    public async Task<IEnumerable<Package>> SearchPackagesAsync(string searchTerm, bool includePrerelease = false)
    {
        var allResults = new List<Package>();
        var enabledSources = GetEnabledSources();
        
        foreach (var source in enabledSources)
        {
            try
            {
                var results = await source.SearchPackagesAsync(searchTerm, includePrerelease);
                allResults.AddRange(results);
            }
            catch (Exception ex)
            {
                // 记录错误但继续搜索其他源
                Console.WriteLine($"Error searching in source '{source.Name}': {ex.Message}");
            }
        }
        
        // 去重（基于包ID和版本）
        return allResults
            .GroupBy(p => new { p.Id, p.Version })
            .Select(g => g.First())
            .OrderBy(p => p.Id)
            .ThenBy(p => p.Version);
    }
}