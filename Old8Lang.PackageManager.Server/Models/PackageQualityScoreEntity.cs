using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Old8Lang.PackageManager.Server.Models;

/// <summary>
/// Entity representing the quality score and metrics for a package version
/// </summary>
public class PackageQualityScoreEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the package entity
    /// </summary>
    public int PackageEntityId { get; set; }

    /// <summary>
    /// Navigation property to the package
    /// </summary>
    [ForeignKey(nameof(PackageEntityId))]
    public PackageEntity Package { get; set; } = null!;

    /// <summary>
    /// Overall quality score (0-100)
    /// </summary>
    [Range(0, 100)]
    public double QualityScore { get; set; }

    /// <summary>
    /// Metadata completeness score (0-100)
    /// Evaluates: description, author, license, homepage, repository, keywords
    /// </summary>
    [Range(0, 100)]
    public double CompletenessScore { get; set; }

    /// <summary>
    /// Stability score (0-100)
    /// Evaluates: prerelease status, version maturity, update frequency
    /// </summary>
    [Range(0, 100)]
    public double StabilityScore { get; set; }

    /// <summary>
    /// Maintenance score (0-100)
    /// Evaluates: recency of updates, update frequency
    /// </summary>
    [Range(0, 100)]
    public double MaintenanceScore { get; set; }

    /// <summary>
    /// Security score (0-100)
    /// Evaluates: package signing, signature verification
    /// </summary>
    [Range(0, 100)]
    public double SecurityScore { get; set; }

    /// <summary>
    /// Community engagement score (0-100)
    /// Evaluates: download count, popularity
    /// </summary>
    [Range(0, 100)]
    public double CommunityScore { get; set; }

    /// <summary>
    /// Documentation quality score (0-100)
    /// Evaluates: presence of description, metadata richness
    /// </summary>
    [Range(0, 100)]
    public double DocumentationScore { get; set; }

    /// <summary>
    /// Timestamp when the quality score was last calculated
    /// </summary>
    public DateTime LastCalculatedAt { get; set; }

    /// <summary>
    /// Timestamp when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
