using Old8Lang.PackageManager.Server.Models;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// Service for calculating and managing package quality scores
/// </summary>
public interface IPackageQualityService
{
    /// <summary>
    /// Calculate quality score for a package
    /// </summary>
    Task<PackageQualityScoreEntity> CalculateQualityScoreAsync(PackageEntity package);

    /// <summary>
    /// Recalculate quality scores for all packages
    /// </summary>
    Task RecalculateAllScoresAsync();

    /// <summary>
    /// Get quality score for a specific package
    /// </summary>
    Task<PackageQualityScoreEntity?> GetQualityScoreAsync(string packageId, string version);
}
