using Microsoft.EntityFrameworkCore;
using Old8Lang.PackageManager.Server.Data;
using Old8Lang.PackageManager.Server.Models;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// 包依赖关系分析服务实现
/// </summary>
public class PackageDependencyService : IPackageDependencyService
{
    private readonly PackageManagerDbContext _context;
    private readonly ILogger<PackageDependencyService> _logger;

    public PackageDependencyService(
        PackageManagerDbContext context,
        ILogger<PackageDependencyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取包的依赖树
    /// </summary>
    public async Task<DependencyTreeResponse> GetDependencyTreeAsync(
        string packageId,
        string version,
        int maxDepth = 10)
    {
        var package = await _context.Packages
            .Include(p => p.PackageDependencies)
            .FirstOrDefaultAsync(p => p.PackageId == packageId && p.Version == version);

        if (package == null)
        {
            throw new ArgumentException($"Package {packageId} version {version} not found");
        }

        var visitedPackages = new HashSet<string>();
        var circularPaths = new List<string>();
        var context = new DependencyTreeBuildContext
        {
            MaxDepthReached = 0
        };

        var rootNode = await BuildDependencyTreeAsync(
            package,
            0,
            maxDepth,
            visitedPackages,
            circularPaths,
            context,
            new List<string> { $"{packageId}@{version}" });

        var totalDependencies = CountTotalDependencies(rootNode);

        return new DependencyTreeResponse
        {
            PackageId = packageId,
            Version = version,
            Language = package.Language,
            TotalDependencies = totalDependencies,
            MaxDepth = context.MaxDepthReached,
            HasCircularDependencies = circularPaths.Count > 0,
            CircularPaths = circularPaths.Distinct().ToList(),
            RootNode = rootNode
        };
    }

    private class DependencyTreeBuildContext
    {
        public int MaxDepthReached { get; set; }
    }

    /// <summary>
    /// 获取包的依赖图
    /// </summary>
    public async Task<DependencyGraphResponse> GetDependencyGraphAsync(
        string packageId,
        string version,
        int maxDepth = 10)
    {
        var treeResponse = await GetDependencyTreeAsync(packageId, version, maxDepth);

        var nodes = new List<DependencyGraphNode>();
        var edges = new List<DependencyGraphEdge>();
        var visitedNodes = new HashSet<string>();

        // 添加根节点
        var rootNodeId = $"{treeResponse.PackageId}@{treeResponse.Version}";
        nodes.Add(new DependencyGraphNode
        {
            Id = rootNodeId,
            PackageId = treeResponse.PackageId,
            Version = treeResponse.Version,
            Language = treeResponse.Language,
            Label = $"{treeResponse.PackageId}\n{treeResponse.Version}",
            Level = 0,
            IsRoot = true,
            IsCircular = false,
            IsDevelopmentDependency = false
        });
        visitedNodes.Add(rootNodeId);

        // 遍历依赖树构建图结构
        BuildDependencyGraph(treeResponse.RootNode, nodes, edges, visitedNodes, treeResponse.CircularPaths);

        return new DependencyGraphResponse
        {
            Nodes = nodes,
            Edges = edges,
            TotalNodes = nodes.Count,
            TotalEdges = edges.Count,
            HasCircularDependencies = treeResponse.HasCircularDependencies,
            CircularPaths = treeResponse.CircularPaths
        };
    }

    /// <summary>
    /// 检测循环依赖
    /// </summary>
    public async Task<List<string>> DetectCircularDependenciesAsync(string packageId, string version)
    {
        var treeResponse = await GetDependencyTreeAsync(packageId, version, 50);
        return treeResponse.CircularPaths;
    }

    /// <summary>
    /// 递归构建依赖树
    /// </summary>
    private async Task<DependencyTreeNode> BuildDependencyTreeAsync(
        PackageEntity package,
        int currentDepth,
        int maxDepth,
        HashSet<string> visitedPackages,
        List<string> circularPaths,
        DependencyTreeBuildContext context,
        List<string> currentPath)
    {
        var packageKey = $"{package.PackageId}@{package.Version}";

        if (currentDepth > context.MaxDepthReached)
        {
            context.MaxDepthReached = currentDepth;
        }

        var node = new DependencyTreeNode
        {
            PackageId = package.PackageId,
            Version = package.Version,
            Language = package.Language,
            VersionRange = package.Version,
            IsResolved = true,
            IsCircular = false,
            IsDevelopmentDependency = false,
            Depth = currentDepth,
            Dependencies = new List<DependencyTreeNode>()
        };

        // 检查是否达到最大深度
        if (currentDepth >= maxDepth)
        {
            _logger.LogWarning("Reached max depth {MaxDepth} for package {PackageKey}", maxDepth, packageKey);
            return node;
        }

        // 检查循环依赖
        if (visitedPackages.Contains(packageKey))
        {
            node.IsCircular = true;
            var circularPath = string.Join(" -> ", currentPath) + $" -> {packageKey}";
            circularPaths.Add(circularPath);
            _logger.LogWarning("Circular dependency detected: {CircularPath}", circularPath);
            return node;
        }

        visitedPackages.Add(packageKey);

        try
        {
            // 获取依赖包
            var dependencies = package.PackageDependencies ?? new List<PackageDependencyEntity>();

            foreach (var dep in dependencies)
            {
                var depPackage = await FindBestMatchingVersionAsync(dep.DependencyId, dep.VersionRange, package.Language);

                if (depPackage != null)
                {
                    var newPath = new List<string>(currentPath) { $"{dep.DependencyId}@{depPackage.Version}" };

                    var childNode = await BuildDependencyTreeAsync(
                        depPackage,
                        currentDepth + 1,
                        maxDepth,
                        new HashSet<string>(visitedPackages),
                        circularPaths,
                        context,
                        newPath);

                    childNode.VersionRange = dep.VersionRange;
                    childNode.IsDevelopmentDependency = false; // PackageDependencyEntity doesn't have this property
                    node.Dependencies.Add(childNode);
                }
                else
                {
                    // 未解析的依赖
                    node.Dependencies.Add(new DependencyTreeNode
                    {
                        PackageId = dep.DependencyId,
                        Version = "?",
                        Language = package.Language,
                        VersionRange = dep.VersionRange,
                        IsResolved = false,
                        IsCircular = false,
                        IsDevelopmentDependency = false,
                        Depth = currentDepth + 1,
                        Dependencies = new List<DependencyTreeNode>()
                    });

                    _logger.LogWarning("Could not resolve dependency {DependencyId} with version range {VersionRange}",
                        dep.DependencyId, dep.VersionRange);
                }
            }
        }
        finally
        {
            visitedPackages.Remove(packageKey);
        }

        return node;
    }

    /// <summary>
    /// 构建依赖图（从树结构转换）
    /// </summary>
    private void BuildDependencyGraph(
        DependencyTreeNode treeNode,
        List<DependencyGraphNode> nodes,
        List<DependencyGraphEdge> edges,
        HashSet<string> visitedNodes,
        List<string> circularPaths)
    {
        var fromNodeId = $"{treeNode.PackageId}@{treeNode.Version}";

        foreach (var dep in treeNode.Dependencies)
        {
            var toNodeId = $"{dep.PackageId}@{dep.Version}";

            // 添加节点（如果尚未添加）
            if (!visitedNodes.Contains(toNodeId))
            {
                var isCircular = circularPaths.Any(path => path.Contains(toNodeId));

                nodes.Add(new DependencyGraphNode
                {
                    Id = toNodeId,
                    PackageId = dep.PackageId,
                    Version = dep.Version,
                    Language = dep.Language,
                    Label = dep.IsResolved ? $"{dep.PackageId}\n{dep.Version}" : $"{dep.PackageId}\n(unresolved)",
                    Level = dep.Depth,
                    IsRoot = false,
                    IsCircular = isCircular,
                    IsDevelopmentDependency = dep.IsDevelopmentDependency
                });

                visitedNodes.Add(toNodeId);

                // 递归处理子依赖
                if (dep.IsResolved && !dep.IsCircular)
                {
                    BuildDependencyGraph(dep, nodes, edges, visitedNodes, circularPaths);
                }
            }

            // 添加边
            edges.Add(new DependencyGraphEdge
            {
                From = fromNodeId,
                To = toNodeId,
                VersionRange = dep.VersionRange,
                IsDevelopmentDependency = dep.IsDevelopmentDependency
            });
        }
    }

    /// <summary>
    /// 查找最佳匹配版本
    /// </summary>
    private async Task<PackageEntity?> FindBestMatchingVersionAsync(
        string packageId,
        string versionRange,
        string language)
    {
        var packages = await _context.Packages
            .Include(p => p.PackageDependencies)
            .Where(p => p.PackageId == packageId && p.Language == language && p.IsListed)
            .OrderByDescending(p => p.PublishedAt)
            .ToListAsync();

        if (!packages.Any())
        {
            return null;
        }

        // 简化版本匹配：如果版本范围包含 '*'，匹配最新版本
        if (versionRange.Contains("*") || versionRange.Contains(">=") || versionRange.Contains("^") || versionRange.Contains("~"))
        {
            return packages.FirstOrDefault();
        }

        // 精确匹配
        var exactMatch = packages.FirstOrDefault(p => p.Version == versionRange);
        if (exactMatch != null)
        {
            return exactMatch;
        }

        // 如果没有精确匹配，返回最新版本
        return packages.FirstOrDefault();
    }

    /// <summary>
    /// 计算总依赖数
    /// </summary>
    private int CountTotalDependencies(DependencyTreeNode node)
    {
        var count = node.Dependencies.Count;

        foreach (var dep in node.Dependencies)
        {
            if (!dep.IsCircular)
            {
                count += CountTotalDependencies(dep);
            }
        }

        return count;
    }
}
