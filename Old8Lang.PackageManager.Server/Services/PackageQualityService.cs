using Microsoft.EntityFrameworkCore;
using Old8Lang.PackageManager.Server.Data;
using Old8Lang.PackageManager.Server.Models;

namespace Old8Lang.PackageManager.Server.Services;

/// <summary>
/// Implementation of package quality scoring service
/// </summary>
public class PackageQualityService : IPackageQualityService
{
    private readonly PackageManagerDbContext _context;
    private readonly ILogger<PackageQualityService> _logger;

    // Weights for different quality dimensions (must sum to 1.0)
    private const double CompletenessWeight = 0.25;
    private const double StabilityWeight = 0.15;
    private const double MaintenanceWeight = 0.15;
    private const double SecurityWeight = 0.20;
    private const double CommunityWeight = 0.15;
    private const double DocumentationWeight = 0.10;

    public PackageQualityService(
        PackageManagerDbContext context,
        ILogger<PackageQualityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PackageQualityScoreEntity> CalculateQualityScoreAsync(PackageEntity package)
    {
        _logger.LogInformation("Calculating quality score for package {PackageId} version {Version}",
            package.PackageId, package.Version);

        // Load related entities if not already loaded
        if (!package.PackageTags.Any())
        {
            await _context.Entry(package)
                .Collection(p => p.PackageTags)
                .LoadAsync();
        }

        if (!package.PackageDependencies.Any())
        {
            await _context.Entry(package)
                .Collection(p => p.PackageDependencies)
                .LoadAsync();
        }

        // Calculate individual dimension scores
        var completeness = CalculateCompletenessScore(package);
        var stability = await CalculateStabilityScoreAsync(package);
        var maintenance = await CalculateMaintenanceScoreAsync(package);
        var security = CalculateSecurityScore(package);
        var community = CalculateCommunityScore(package);
        var documentation = CalculateDocumentationScore(package);

        // Calculate weighted overall score
        var overallScore =
            (completeness * CompletenessWeight) +
            (stability * StabilityWeight) +
            (maintenance * MaintenanceWeight) +
            (security * SecurityWeight) +
            (community * CommunityWeight) +
            (documentation * DocumentationWeight);

        var qualityScore = new PackageQualityScoreEntity
        {
            PackageEntityId = package.Id,
            QualityScore = Math.Round(overallScore, 2),
            CompletenessScore = Math.Round(completeness, 2),
            StabilityScore = Math.Round(stability, 2),
            MaintenanceScore = Math.Round(maintenance, 2),
            SecurityScore = Math.Round(security, 2),
            CommunityScore = Math.Round(community, 2),
            DocumentationScore = Math.Round(documentation, 2),
            LastCalculatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Quality score calculated: Overall={Overall}, Completeness={Completeness}, " +
            "Stability={Stability}, Maintenance={Maintenance}, Security={Security}, " +
            "Community={Community}, Documentation={Documentation}",
            qualityScore.QualityScore, qualityScore.CompletenessScore,
            qualityScore.StabilityScore, qualityScore.MaintenanceScore,
            qualityScore.SecurityScore, qualityScore.CommunityScore,
            qualityScore.DocumentationScore);

        return qualityScore;
    }

    private double CalculateCompletenessScore(PackageEntity package)
    {
        var score = 0.0;
        var maxScore = 7.0;

        // Description (20%)
        if (!string.IsNullOrWhiteSpace(package.Description) && package.Description.Length >= 20)
            score += 1.4;
        else if (!string.IsNullOrWhiteSpace(package.Description))
            score += 0.7;

        // Author (15%)
        if (!string.IsNullOrWhiteSpace(package.Author))
            score += 1.05;

        // License (15%)
        if (!string.IsNullOrWhiteSpace(package.License))
            score += 1.05;

        // Project URL (15%)
        if (!string.IsNullOrWhiteSpace(package.ProjectUrl))
            score += 1.05;

        // Tags/Keywords (20%)
        var tagCount = package.PackageTags.Count;
        if (tagCount >= 5)
            score += 1.4;
        else if (tagCount >= 3)
            score += 1.0;
        else if (tagCount >= 1)
            score += 0.5;

        // Package ID quality (10%)
        if (IsGoodPackageId(package.PackageId))
            score += 0.7;

        // Version format (5%)
        if (IsValidSemanticVersion(package.Version))
            score += 0.35;

        return (score / maxScore) * 100.0;
    }

    private async Task<double> CalculateStabilityScoreAsync(PackageEntity package)
    {
        var score = 0.0;

        // Prerelease penalty (40%)
        if (!package.IsPrerelease)
            score += 40.0;

        // Version maturity (30%)
        // Check if there are multiple stable versions
        var stableVersionCount = await _context.Packages
            .Where(p => p.PackageId == package.PackageId && !p.IsPrerelease)
            .CountAsync();

        if (stableVersionCount >= 5)
            score += 30.0;
        else if (stableVersionCount >= 3)
            score += 20.0;
        else if (stableVersionCount >= 1)
            score += 10.0;

        // Package age (30%)
        var packageAge = DateTime.UtcNow - package.PublishedAt;
        if (packageAge.TotalDays >= 365)
            score += 30.0;
        else if (packageAge.TotalDays >= 180)
            score += 20.0;
        else if (packageAge.TotalDays >= 90)
            score += 10.0;
        else if (packageAge.TotalDays >= 30)
            score += 5.0;

        return score;
    }

    private async Task<double> CalculateMaintenanceScoreAsync(PackageEntity package)
    {
        var score = 0.0;

        // Recency of updates (50%)
        var daysSinceUpdate = (DateTime.UtcNow - package.UpdatedAt).TotalDays;
        if (daysSinceUpdate <= 30)
            score += 50.0;
        else if (daysSinceUpdate <= 90)
            score += 40.0;
        else if (daysSinceUpdate <= 180)
            score += 30.0;
        else if (daysSinceUpdate <= 365)
            score += 20.0;
        else if (daysSinceUpdate <= 730)
            score += 10.0;

        // Update frequency (50%)
        var allVersions = await _context.Packages
            .Where(p => p.PackageId == package.PackageId)
            .OrderBy(p => p.PublishedAt)
            .ToListAsync();

        if (allVersions.Count >= 2)
        {
            var totalDays = (allVersions.Last().PublishedAt - allVersions.First().PublishedAt).TotalDays;
            if (totalDays > 0)
            {
                var updatesPerYear = (allVersions.Count - 1) / (totalDays / 365.0);

                if (updatesPerYear >= 12) // Monthly or more
                    score += 50.0;
                else if (updatesPerYear >= 6) // Bi-monthly
                    score += 40.0;
                else if (updatesPerYear >= 4) // Quarterly
                    score += 30.0;
                else if (updatesPerYear >= 2) // Bi-annual
                    score += 20.0;
                else if (updatesPerYear >= 1) // Annual
                    score += 10.0;
            }
        }

        return score;
    }

    private double CalculateSecurityScore(PackageEntity package)
    {
        var score = 0.0;

        // Package signing (70%)
        if (package.IsSigned)
        {
            score += 70.0;

            // Bonus for having signer information
            if (!string.IsNullOrWhiteSpace(package.SignedBy))
                score += 10.0;
        }

        // Checksum presence (20%)
        if (!string.IsNullOrWhiteSpace(package.Checksum))
            score += 20.0;

        return Math.Min(score, 100.0);
    }

    private double CalculateCommunityScore(PackageEntity package)
    {
        var score = 0.0;

        // Download count (100%)
        var downloads = package.DownloadCount;

        if (downloads >= 10000)
            score = 100.0;
        else if (downloads >= 5000)
            score = 90.0;
        else if (downloads >= 1000)
            score = 80.0;
        else if (downloads >= 500)
            score = 70.0;
        else if (downloads >= 100)
            score = 60.0;
        else if (downloads >= 50)
            score = 50.0;
        else if (downloads >= 10)
            score = 40.0;
        else if (downloads >= 5)
            score = 30.0;
        else if (downloads >= 1)
            score = 20.0;
        else
            score = 0.0;

        return score;
    }

    private double CalculateDocumentationScore(PackageEntity package)
    {
        var score = 0.0;

        // Description quality (50%)
        if (!string.IsNullOrWhiteSpace(package.Description))
        {
            var descLength = package.Description.Length;
            if (descLength >= 200)
                score += 50.0;
            else if (descLength >= 100)
                score += 40.0;
            else if (descLength >= 50)
                score += 30.0;
            else if (descLength >= 20)
                score += 20.0;
            else
                score += 10.0;
        }

        // Project URL presence (30%)
        if (!string.IsNullOrWhiteSpace(package.ProjectUrl))
            score += 30.0;

        // Rich metadata (20%)
        var metadataScore = 0;
        if (!string.IsNullOrWhiteSpace(package.Author)) metadataScore += 5;
        if (!string.IsNullOrWhiteSpace(package.License)) metadataScore += 5;
        if (package.PackageTags.Any()) metadataScore += 10;

        score += metadataScore;

        return Math.Min(score, 100.0);
    }

    private bool IsGoodPackageId(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return false;

        // Good package IDs: lowercase, hyphen/dot separated, descriptive (>= 3 chars)
        return packageId.Length >= 3 &&
               packageId == packageId.ToLowerInvariant() &&
               !packageId.Contains(" ") &&
               char.IsLetter(packageId[0]);
    }

    private bool IsValidSemanticVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        // Basic semantic versioning check: X.Y.Z or X.Y.Z-prerelease
        var parts = version.Split('-')[0].Split('.');
        return parts.Length >= 2 && parts.Length <= 3 &&
               parts.All(p => int.TryParse(p, out _));
    }

    public async Task<PackageQualityScoreEntity?> GetQualityScoreAsync(string packageId, string version)
    {
        var package = await _context.Packages
            .Include(p => p.QualityScore)
            .FirstOrDefaultAsync(p => p.PackageId == packageId && p.Version == version);

        return package?.QualityScore;
    }

    public async Task RecalculateAllScoresAsync()
    {
        _logger.LogInformation("Starting recalculation of all package quality scores");

        var packages = await _context.Packages
            .Include(p => p.PackageTags)
            .Include(p => p.PackageDependencies)
            .Include(p => p.QualityScore)
            .ToListAsync();

        var updatedCount = 0;
        var createdCount = 0;

        foreach (var package in packages)
        {
            try
            {
                var newScore = await CalculateQualityScoreAsync(package);

                if (package.QualityScore != null)
                {
                    // Update existing score
                    package.QualityScore.QualityScore = newScore.QualityScore;
                    package.QualityScore.CompletenessScore = newScore.CompletenessScore;
                    package.QualityScore.StabilityScore = newScore.StabilityScore;
                    package.QualityScore.MaintenanceScore = newScore.MaintenanceScore;
                    package.QualityScore.SecurityScore = newScore.SecurityScore;
                    package.QualityScore.CommunityScore = newScore.CommunityScore;
                    package.QualityScore.DocumentationScore = newScore.DocumentationScore;
                    package.QualityScore.LastCalculatedAt = newScore.LastCalculatedAt;
                    updatedCount++;
                }
                else
                {
                    // Create new score
                    _context.PackageQualityScores.Add(newScore);
                    createdCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating quality score for package {PackageId} version {Version}",
                    package.PackageId, package.Version);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Completed recalculation of quality scores. Created: {Created}, Updated: {Updated}",
            createdCount, updatedCount);
    }
}
