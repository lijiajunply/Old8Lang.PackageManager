using System.Text.Json;
using Old8Lang.PackageManager.Server.Models;
using System.Text.RegularExpressions;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// Python 包解析服务
/// </summary>
public interface IPythonPackageParser
{
    Task<PythonPackageInfo?> ParsePackageAsync(Stream packageStream, string fileName);
    Task<List<ExternalDependencyInfo>> ParseRequirementsAsync(Stream requirementsStream);
    Task<bool> ValidatePythonPackageAsync(Stream packageStream);
    string GetLanguageFromExtension(string fileName);
}

/// <summary>
/// Python 包解析服务实现
/// </summary>
public class PythonPackageParser(ILogger<PythonPackageParser> logger) : IPythonPackageParser
{
    // Python 版本正则表达式
    private static readonly Regex PythonVersionRegex = new(@"^\d+\.\d+(\.\d+)?([ab]|rc|alpha|beta|pre|post|dev)\d*$", RegexOptions.Compiled);

    public async Task<PythonPackageInfo?> ParsePackageAsync(Stream packageStream, string fileName)
    {
        try
        {
            // Python 包通常是 .whl (wheel) 或 .tar.gz 格式
            if (!IsPythonPackage(fileName))
            {
                logger.LogWarning("文件不是有效的 Python 包: {FileName}", fileName);
                return null;
            }
            
            var packageInfo = new PythonPackageInfo();
            
            if (fileName.EndsWith(".whl"))
            {
                packageInfo = await ParseWheelFileAsync(packageStream, fileName);
            }
            else if (fileName.EndsWith(".tar.gz"))
            {
                packageInfo = await ParseSourceDistributionAsync(packageStream, fileName);
            }
            else
            {
                logger.LogWarning("不支持的 Python 包格式: {FileName}", fileName);
                return null;
            }
            
            return packageInfo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "解析 Python 包失败: {FileName}", fileName);
            return null;
        }
    }
    
    public async Task<List<ExternalDependencyInfo>> ParseRequirementsAsync(Stream requirementsStream)
    {
        var dependencies = new List<ExternalDependencyInfo>();
        
        try
        {
            using var reader = new StreamReader(requirementsStream);
            var content = await reader.ReadToEndAsync();
            
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // 跳过注释和空行
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                    continue;
                
                // 跳过 -r, -e 等选项
                if (trimmedLine.StartsWith('-'))
                    continue;
                
                var dependency = ParseRequirementLine(trimmedLine);
                if (dependency != null)
                {
                    dependencies.Add(dependency);
                }
            }
            
            return dependencies;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "解析 requirements.txt 失败");
            return dependencies;
        }
    }
    
    public async Task<bool> ValidatePythonPackageAsync(Stream packageStream)
    {
        try
        {
            // 基本验证：检查文件格式
            packageStream.Position = 0;
            var buffer = new byte[1024];
            _ = await packageStream.ReadAsync(buffer, 0, buffer.Length);
            packageStream.Position = 0;
            
            // ZIP 格式检查 (wheel 和 source distribution 都是 ZIP 格式)
            var isZip = buffer[0] == 0x50 && buffer[1] == 0x4B && 
                       buffer[2] == 0x03 && buffer[3] == 0x04;
            
            return isZip;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "验证 Python 包失败");
            return false;
        }
    }
    
    public string GetLanguageFromExtension(string fileName)
    {
        return fileName.ToLowerInvariant() switch
        {
            var f when f.EndsWith(".whl") || f.EndsWith(".tar.gz") => "python",
            var f when f.EndsWith(".o8pkg") => "old8lang",
            _ => "unknown"
        };
    }
    
    private async Task<PythonPackageInfo> ParseWheelFileAsync(Stream packageStream, string fileName)
    {
        var packageInfo = new PythonPackageInfo();
        
        // 从文件名解析包名和版本
        // 格式: {distribution}-{version}(-{build tag})?-{python tag}-{abi tag}-{platform tag}.whl
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var parts = fileNameWithoutExt.Split('-');
        
        if (parts.Length >= 4) // 至少需要: 包名, 版本, python-tag, abi-tag, platform-tag
        {
            // 从版本部分开始，找到最后一个看起来像版本的部分
            // 版本通常遵循 semantic versioning 格式
            var versionPart = -1;
            for (int i = parts.Length - 4; i >= 1; i--) // 从倒数第4个开始向前找版本
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(parts[i], @"^\d+\.\d+(\.\d+)?(([ab]|rc|alpha|beta|pre|post|dev)\d*)?$"))
                {
                    versionPart = i;
                    break;
                }
            }
            
            if (versionPart > 0)
            {
                packageInfo.Version = parts[versionPart];
                packageInfo.PackageId = string.Join("-", parts.Take(versionPart)).Replace('_', '-');
            }
            else
            {
                // 如果找不到版本格式，假设倒数第3个是版本
                packageInfo.Version = parts[parts.Length - 4];
                packageInfo.PackageId = string.Join("-", parts.Take(parts.Length - 4)).Replace('_', '-');
            }
        }
        
        // 尝试从包内的 METADATA 文件读取详细信息
        try
        {
            var metadata = await ExtractWheelMetadataAsync(packageStream);
            if (metadata != null)
            {
                packageInfo.Description = metadata.Description;
                packageInfo.Author = metadata.Author;
                packageInfo.License = metadata.License;
                packageInfo.ProjectUrl = metadata.ProjectUrl;
                packageInfo.Summary = metadata.Summary;
                packageInfo.Keywords = metadata.Keywords?.ToList() ?? new List<string>();
                packageInfo.Classifiers = metadata.Classifiers?.ToList() ?? new List<string>();
                packageInfo.RequiresPython = metadata.RequiresPython;
                
                // 解析依赖
                if (metadata.RequiresDist != null)
                {
                    foreach (var req in metadata.RequiresDist)
                    {
                        var dep = ParseRequirementLine(req);
                        if (dep != null)
                        {
                            packageInfo.Dependencies.Add(dep);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "无法提取 wheel 文件的元数据，使用文件名解析");
        }
        
        return packageInfo;
    }
    
    private async Task<PythonWheelMetadata?> ExtractWheelMetadataAsync(Stream packageStream)
    {
        try
        {
            // 简化实现：实际应该解压 ZIP 文件并读取 *.dist-info/METADATA
            // 这里返回模拟数据
            return new PythonWheelMetadata
            {
                Summary = "Python package",
                Description = "A Python package",
                Author = "Unknown",
                License = "MIT",
                ProjectUrl = "",
                RequiresDist = new List<string>(),
                RequiresPython = ">=3.6"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "提取 wheel 元数据失败");
            return null;
        }
    }
    
    private async Task<PythonPackageInfo> ParseSourceDistributionAsync(Stream packageStream, string fileName)
    {
        var packageInfo = new PythonPackageInfo();
        
        // 从文件名解析包名和版本
        // 格式: {package}-{version}.tar.gz
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (fileNameWithoutExt.EndsWith(".tar"))
        {
            fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileNameWithoutExt);
        }
        
        var lastDashIndex = fileNameWithoutExt.LastIndexOf('-');
        if (lastDashIndex > 0)
        {
            packageInfo.PackageId = fileNameWithoutExt.Substring(0, lastDashIndex).Replace('_', '-');
            packageInfo.Version = fileNameWithoutExt.Substring(lastDashIndex + 1);
        }
        
        // 尝试从 PKG-INFO 文件读取元数据
        try
        {
            var metadata = await ExtractSourceMetadataAsync(packageStream);
            if (metadata != null)
            {
                packageInfo.Description = metadata.Description;
                packageInfo.Author = metadata.Author;
                packageInfo.License = metadata.License;
                packageInfo.ProjectUrl = metadata.ProjectUrl;
                packageInfo.Summary = metadata.Summary;
                packageInfo.Keywords = metadata.Keywords?.ToList() ?? new List<string>();
                packageInfo.RequiresPython = metadata.RequiresPython;
                
                // 解析依赖
                if (metadata.RequiresDist != null)
                {
                    foreach (var req in metadata.RequiresDist)
                    {
                        var dep = ParseRequirementLine(req);
                        if (dep != null)
                        {
                            packageInfo.Dependencies.Add(dep);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "无法提取源码包的元数据");
        }
        
        return packageInfo;
    }
    
    private async Task<PythonSourceMetadata?> ExtractSourceMetadataAsync(Stream packageStream)
    {
        try
        {
            // 简化实现：实际应该解压 tar.gz 文件并读取 PKG-INFO 或 setup.py
            // 这里返回模拟数据
            return new PythonSourceMetadata
            {
                Summary = "Python source package",
                Description = "A Python source package",
                Author = "Unknown",
                License = "MIT",
                ProjectUrl = "",
                RequiresDist = new List<string>(),
                RequiresPython = ">=3.6"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "提取源码包元数据失败");
            return null;
        }
    }
    
    private ExternalDependencyInfo? ParseRequirementLine(string line)
    {
        try
        {
            // 简化的 requirement 解析
            // 格式: package[extras]==version ; python_version
            var trimmedLine = line.Split(';')[0].Trim(); // 移除环境标记
            
            // 移除注释
            var commentIndex = trimmedLine.IndexOf('#');
            if (commentIndex >= 0)
            {
                trimmedLine = trimmedLine.Substring(0, commentIndex).Trim();
            }
            
            if (string.IsNullOrEmpty(trimmedLine))
                return null;
            
            // 解析包名和版本规范
            var match = Regex.Match(trimmedLine, @"^([a-zA-Z0-9_\-]+)(\[.*?\])?(.*)$");
            if (!match.Success)
                return null;
            
            var packageName = match.Groups[1].Value.Replace('_', '-');
            var extras = match.Groups[2].Value;
            var versionSpec = match.Groups[3].Value.Trim();
            
            if (string.IsNullOrEmpty(versionSpec))
            {
                versionSpec = "*";
            }
            
            return new ExternalDependencyInfo
            {
                DependencyType = "pip",
                PackageName = packageName,
                VersionSpec = versionSpec,
                IndexUrl = "",
                ExtraIndexUrl = "",
                IsDevDependency = false
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "解析依赖行失败: {Line}", line);
            return null;
        }
    }
    
    private bool IsPythonPackage(string fileName)
    {
        var lowerFileName = fileName.ToLowerInvariant();
        return lowerFileName.EndsWith(".whl") || lowerFileName.EndsWith(".tar.gz");
    }
}

/// <summary>
/// Python 包信息
/// </summary>
public class PythonPackageInfo
{
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public List<string> Classifiers { get; set; } = new();
    public string RequiresPython { get; set; } = string.Empty;
    public List<ExternalDependencyInfo> Dependencies { get; set; } = new();
}

/// <summary>
/// Python Wheel 元数据
/// </summary>
public class PythonWheelMetadata
{
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public List<string>? Keywords { get; set; }
    public List<string>? Classifiers { get; set; }
    public string RequiresPython { get; set; } = string.Empty;
    public List<string>? RequiresDist { get; set; }
}

/// <summary>
/// Python 源码包元数据
/// </summary>
public class PythonSourceMetadata
{
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public List<string>? Keywords { get; set; }
    public string RequiresPython { get; set; } = string.Empty;
    public List<string>? RequiresDist { get; set; }
}