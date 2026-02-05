using Transcendence.Service.Core.Services.Analytics.Models;

namespace Transcendence.Service.Core.Services.Analytics.Interfaces;

/// <summary>
/// Raw computation service for champion analytics.
/// Performs EF Core aggregation queries without caching.
/// </summary>
public interface IChampionAnalyticsComputeService
{
    /// <summary>
    /// Computes win rates for a champion across roles and rank tiers.
    /// Only returns data for combinations with sufficient sample size (100+ games).
    /// </summary>
    Task<List<ChampionWinRateDto>> ComputeWinRatesAsync(
        int championId,
        ChampionAnalyticsFilter filter,
        string patch,
        CancellationToken ct);

    /// <summary>
    /// Computes tier list ranking champions by composite score (70% win rate + 30% pick rate).
    /// Assigns S/A/B/C/D grades by percentile: S=top 10%, A=10-30%, B=30-60%, C=60-85%, D=85%+
    /// </summary>
    Task<List<TierListEntry>> ComputeTierListAsync(
        string? role,
        string? rankTier,
        string patch,
        CancellationToken ct);
}
