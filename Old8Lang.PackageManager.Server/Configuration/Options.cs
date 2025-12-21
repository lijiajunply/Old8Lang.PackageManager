namespace Old8Lang.PackageManager.Server.Configuration;

/// <summary>
/// 包存储配置
/// </summary>
public class PackageStorageOptions
{
    /// <summary>
    /// 包存储根目录
    /// </summary>
    public string StoragePath { get; set; } = "packages";
    
    /// <summary>
    /// 最大包大小 (字节)
    /// </summary>
    public long MaxPackageSize { get; set; } = 100 * 1024 * 1024; // 100MB
    
    /// <summary>
    /// 允许的包文件扩展名
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new() { ".o8pkg", ".tgz", ".tar.gz" };
    
    /// <summary>
    /// 是否启用压缩
    /// </summary>
    public bool EnableCompression { get; set; } = true;
    
    /// <summary>
    /// 语言特定的存储路径
    /// </summary>
    public Dictionary<string, string> LanguagePaths { get; set; } = new()
    {
        ["old8lang"] = "packages/old8lang",
        ["python"] = "packages/python", 
        ["javascript"] = "packages/javascript",
        ["typescript"] = "packages/typescript"
    };
}

/// <summary>
/// API 配置
/// </summary>
public class ApiOptions
{
    /// <summary>
    /// API 版本
    /// </summary>
    public string Version { get; set; } = "3.0.0";
    
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = "Old8Lang Package Manager";
    
    /// <summary>
    /// 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:5001";
    
    /// <summary>
    /// 是否启用 API 密钥验证
    /// </summary>
    public bool RequireApiKey { get; set; } = true;
    
    /// <summary>
    /// 速率限制 (每分钟请求数)
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 100;
    
    /// <summary>
    /// 支持的语言列表
    /// </summary>
    public List<string> SupportedLanguages { get; set; } = new() { "old8lang", "python", "javascript", "typescript" };
    
    /// <summary>
    /// 默认语言
    /// </summary>
    public string DefaultLanguage { get; set; } = "old8lang";
}

/// <summary>
/// NPM 注册表配置
/// </summary>
public class NpmRegistryOptions
{
    /// <summary>
    /// 是否启用 NPM 兼容性
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// NPM 注册表基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://registry.npmjs.org";
    
    /// <summary>
    /// 是否重定向到官方 NPM
    /// </summary>
    public bool RedirectToOfficialNpm { get; set; } = false;
    
    /// <summary>
    /// 支持的包格式
    /// </summary>
    public List<string> SupportedFormats { get; set; } = new() { ".tgz", ".tar.gz" };
}

/// <summary>
/// 安全配置
/// </summary>
public class SecurityOptions
{
    /// <summary>
    /// 是否启用包签名验证
    /// </summary>
    public bool EnablePackageSigning { get; set; } = false;
    
    /// <summary>
    /// 信任的签名证书指纹
    /// </summary>
    public List<string> TrustedCertificates { get; set; } = new();
    
    /// <summary>
    /// 是否校验包完整性
    /// </summary>
    public bool EnableChecksumValidation { get; set; } = true;
    
    /// <summary>
    /// 允许的算法
    /// </summary>
    public List<string> AllowedHashAlgorithms { get; set; } = new() { "SHA256", "SHA512" };
}