using Old8Lang.PackageManager.Core.Interfaces;

namespace Old8Lang.PackageManager.Core.Services;

/// <summary>
/// 版本管理器 - 处理版本比较和语义化版本
/// </summary>
public class VersionManager
{
    /// <summary>
    /// 比较两个版本字符串
    /// </summary>
    /// <returns>-1 if version1 &lt; version2, 0 if equal, 1 if version1 > version2</returns>
    public int CompareVersions(string version1, string version2)
    {
        var v1 = ParseVersion(version1);
        var v2 = ParseVersion(version2);

        // 比较主版本
        if (v1.Major != v2.Major)
            return v1.Major.CompareTo(v2.Major);

        // 比较次版本
        if (v1.Minor != v2.Minor)
            return v1.Minor.CompareTo(v2.Minor);

        // 比较修订版本
        if (v1.Patch != v2.Patch)
            return v1.Patch.CompareTo(v2.Patch);

        // 比较预发布标识符
        if (v1.IsPrerelease && !v2.IsPrerelease)
            return -1; // 预发布版本小于正式版本

        if (!v1.IsPrerelease && v2.IsPrerelease)
            return 1; // 正式版本大于预发布版本

        if (v1.IsPrerelease && v2.IsPrerelease)
        {
            return string.Compare(v1.Prerelease, v2.Prerelease, StringComparison.OrdinalIgnoreCase);
        }

        return 0;
    }

    /// <summary>
    /// 检查版本是否在指定范围内
    /// </summary>
    public bool IsVersionInRange(string version, string versionRange)
    {
        var range = ParseVersionRange(versionRange);
        var targetVersion = ParseVersion(version);

        // 检查最小版本
        if (!string.IsNullOrEmpty(range.MinVersion))
        {
            var minVersion = ParseVersion(range.MinVersion);
            var minComparison = CompareVersions(version, range.MinVersion);

            if (range.IncludeMinVersion)
            {
                if (minComparison < 0) return false;
            }
            else
            {
                if (minComparison <= 0) return false;
            }
        }

        // 检查最大版本
        if (!string.IsNullOrEmpty(range.MaxVersion))
        {
            var maxComparison = CompareVersions(version, range.MaxVersion);

            if (range.IncludeMaxVersion)
            {
                if (maxComparison > 0) return false;
            }
            else
            {
                if (maxComparison >= 0) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 获取范围内的最新版本
    /// </summary>
    public string? GetLatestVersionInRange(IEnumerable<string> versions, string versionRange)
    {
        return versions
            .Where(v => IsVersionInRange(v, versionRange))
            .OrderByDescending(v => v, new VersionComparer())
            .FirstOrDefault();
    }

    /// <summary>
    /// 解析版本字符串为结构化版本
    /// </summary>
    public SemanticVersion ParseVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return new SemanticVersion();

        var parts = version.Split('-', 2);
        var versionPart = parts[0];
        var prereleasePart = parts.Length > 1 ? parts[1] : string.Empty;

        var versionNumbers = versionPart.Split('.');
        var semanticVersion = new SemanticVersion
        {
            IsPrerelease = !string.IsNullOrEmpty(prereleasePart),
            Prerelease = prereleasePart
        };

        if (versionNumbers.Length > 0 && int.TryParse(versionNumbers[0], out var major))
            semanticVersion.Major = major;

        if (versionNumbers.Length > 1 && int.TryParse(versionNumbers[1], out var minor))
            semanticVersion.Minor = minor;

        if (versionNumbers.Length > 2 && int.TryParse(versionNumbers[2], out var patch))
            semanticVersion.Patch = patch;

        return semanticVersion;
    }

    /// <summary>
    /// 解析版本范围字符串
    /// </summary>
    public VersionRange ParseVersionRange(string versionRange)
    {
        var range = new VersionRange();

        if (string.IsNullOrWhiteSpace(versionRange))
            return range;

        versionRange = versionRange.Trim();

        // 处理通配符版本 (例如: "1.2.*")
        if (versionRange.EndsWith(".*"))
        {
            var baseVersion = versionRange[..^2];
            range.MinVersion = baseVersion;
            range.MaxVersion = baseVersion + ".999";
            range.IncludeMinVersion = true;
            range.IncludeMaxVersion = true;
        }
        // 处理范围版本 (例如: "1.2.0-2.0.0")
        else if (versionRange.Contains('-'))
        {
            var parts = versionRange.Split('-', 2);
            range.MinVersion = parts[0].Trim();
            range.MaxVersion = parts[1].Trim();
            range.IncludeMinVersion = true;
            range.IncludeMaxVersion = true;
        }
        // 处理比较版本 (例如: ">=1.2.0")
        else if (versionRange.StartsWith(">="))
        {
            range.MinVersion = versionRange[2..].Trim();
            range.IncludeMinVersion = true;
        }
        else if (versionRange.StartsWith(">"))
        {
            range.MinVersion = versionRange[1..].Trim();
            range.IncludeMinVersion = false;
        }
        else if (versionRange.StartsWith("<="))
        {
            range.MaxVersion = versionRange[2..].Trim();
            range.IncludeMaxVersion = true;
        }
        else if (versionRange.StartsWith("<"))
        {
            range.MaxVersion = versionRange[1..].Trim();
            range.IncludeMaxVersion = false;
        }
        // 处理精确版本
        else
        {
            range.MinVersion = versionRange;
            range.MaxVersion = versionRange;
            range.IncludeMinVersion = true;
            range.IncludeMaxVersion = true;
        }

        return range;
    }
}

/// <summary>
/// 语义化版本
/// </summary>
public class SemanticVersion
{
    /// <summary>
    /// 主版本
    /// </summary>
    public int Major { get; set; }

    /// <summary>
    /// 次要版本
    /// </summary>
    public int Minor { get; set; }

    /// <summary>
    /// 补丁版本
    /// </summary>
    public int Patch { get; set; }

    /// <summary>
    /// 是否为预发布版本
    /// </summary>
    public bool IsPrerelease { get; set; }

    /// <summary>
    /// 预发布版本
    /// </summary>
    public string Prerelease { get; set; } = string.Empty;

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (IsPrerelease && !string.IsNullOrEmpty(Prerelease))
        {
            version += $"-{Prerelease}";
        }

        return version;
    }
}

/// <summary>
/// 版本比较器
/// </summary>
public class VersionComparer : IComparer<string>
{
    private readonly VersionManager _versionManager;

    /// <summary>
    /// 
    /// </summary>
    public VersionComparer()
    {
        _versionManager = new VersionManager();
    }

    /// <summary>
    /// 比较版本
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        return _versionManager.CompareVersions(x, y);
    }
}