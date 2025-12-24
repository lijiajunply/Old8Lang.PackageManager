using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Old8Lang.PackageManager.Core.Models;

namespace Old8Lang.PackageManager.Server.Models;

/// <summary>
/// 包信息实体
/// </summary>
[Serializable]
public class PackageEntity
{
    [Key] public int Id { get; set; }

    [Required] [MaxLength(200)] public string PackageId { get; set; } = string.Empty;

    [Required] [MaxLength(50)] public string Version { get; set; } = string.Empty;

    [Required] [MaxLength(20)] public string Language { get; set; } = "old8lang";

    [MaxLength(1000)] public string Description { get; set; } = string.Empty;

    [MaxLength(200)] public string Author { get; set; } = string.Empty;

    [MaxLength(500)] public string License { get; set; } = string.Empty;

    [MaxLength(500)] public string ProjectUrl { get; set; } = string.Empty;

    [MaxLength(1000)] public string Checksum { get; set; } = string.Empty;

    public long Size { get; set; }

    public DateTime PublishedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int DownloadCount { get; set; }

    public bool IsListed { get; set; } = true;

    public bool IsPrerelease { get; set; }

    [NotMapped] public List<string> Tags { get; set; } = new();

    [NotMapped] public List<PackageDependency> Dependencies { get; set; } = new();

    // 导航属性
    public virtual ICollection<PackageTagEntity> PackageTags { get; set; } = new List<PackageTagEntity>();

    public virtual ICollection<PackageDependencyEntity> PackageDependencies { get; set; } =
        new List<PackageDependencyEntity>();

    public virtual ICollection<PackageFileEntity> Files { get; set; } = new List<PackageFileEntity>();

    public virtual ICollection<ExternalDependencyEntity> ExternalDependencies { get; set; } =
        new List<ExternalDependencyEntity>();

    public virtual ICollection<LanguageMetadataEntity> LanguageMetadata { get; set; } =
        new List<LanguageMetadataEntity>();
}

/// <summary>
/// 包标签实体
/// </summary>
[Serializable]
public class PackageTagEntity
{
    [Key] public int Id { get; set; }

    [Required] [MaxLength(100)] public string Tag { get; set; } = string.Empty;

    public int PackageEntityId { get; set; }

    public virtual PackageEntity Package { get; set; } = null!;
}

/// <summary>
/// 包依赖实体
/// </summary>
[Serializable]
public class PackageDependencyEntity
{
    [Key] public int Id { get; set; }

    [Required] [MaxLength(200)] public string DependencyId { get; set; } = string.Empty;

    [Required] [MaxLength(100)] public string VersionRange { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;

    [MaxLength(50)] public string TargetFramework { get; set; } = string.Empty;

    public int PackageEntityId { get; set; }

    public virtual PackageEntity Package { get; set; } = null!;
}

/// <summary>
/// 包文件实体
/// </summary>
[Serializable]
public class PackageFileEntity
{
    [Key] public int Id { get; set; }

    [Required] [MaxLength(500)] public string FileName { get; set; } = string.Empty;

    [Required] [MaxLength(500)] public string FilePath { get; set; } = string.Empty;

    [Required] public long FileSize { get; set; }

    [Required] [MaxLength(100)] public string ContentType { get; set; } = string.Empty;

    [MaxLength(1000)] public string Checksum { get; set; } = string.Empty;

    public int PackageEntityId { get; set; }

    public virtual PackageEntity Package { get; set; } = null!;
}

/// <summary>
/// API 密钥实体
/// </summary>
[Serializable]
public class ApiKeyEntity
{
    [Key] public int Id { get; set; }

    [Required] [MaxLength(200)] public string Name { get; set; } = string.Empty;

    [Required] [MaxLength(500)] public string Key { get; set; } = string.Empty;

    [MaxLength(1000)] public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(50)] public string Scopes { get; set; } = "package:read,package:write";

    public int UsageCount { get; set; }
}

/// <summary>
/// 外部依赖实体（用于 Python 包等）
/// </summary>
[Serializable]
public class ExternalDependencyEntity
{
    [Key] public int Id { get; set; }

    [Required] [MaxLength(100)] public string DependencyType { get; set; } = string.Empty; // pip, conda, etc.

    [Required] [MaxLength(200)] public string PackageName { get; set; } = string.Empty;

    [Required] [MaxLength(100)] public string VersionSpec { get; set; } = string.Empty;

    [MaxLength(50)] public string IndexUrl { get; set; } = string.Empty;

    [MaxLength(50)] public string ExtraIndexUrl { get; set; } = string.Empty;

    public bool IsDevDependency { get; set; }

    public int PackageEntityId { get; set; }

    public virtual PackageEntity Package { get; set; } = null!;
}

/// <summary>
/// 语言特定的元数据
/// </summary>
[Serializable]
public class LanguageMetadataEntity
{
    [Key] public int Id { get; set; }

    [Required] [MaxLength(20)] public string Language { get; set; } = string.Empty; // old8lang, python

    [Required] [MaxLength(1000)] public string Metadata { get; set; } = string.Empty; // JSON 格式的特定元数据

    public DateTime UpdatedAt { get; set; }

    public int PackageEntityId { get; set; }

    public virtual PackageEntity Package { get; set; } = null!;
}