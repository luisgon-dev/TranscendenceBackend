using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Transcendence.Data;
using Transcendence.Service.Core.Services.Analytics.Interfaces;
using Transcendence.Service.Core.Services.Analytics.Models;

namespace Transcendence.Service.Core.Services.Jobs;

/// <summary>
/// Daily job to refresh champion analytics cache.
/// Runs at 4 AM UTC to minimize user impact.
/// </summary>
public class RefreshChampionAnalyticsJob(
    IChampionAnalyticsService analyticsService,
    TranscendenceContext db,
    ILogger<RefreshChampionAnalyticsJob> logger)
{
    // Popular roles to pre-warm
    private static readonly string[] Roles = { "TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY" };

    // Primary rank tiers to pre-warm (covers majority of player base)
    private static readonly string[] PrimaryTiers = { "Gold", "Platinum", "Emerald", "Diamond" };

    public async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting daily champion analytics refresh");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Step 1: Invalidate all analytics cache
            logger.LogInformation("Invalidating analytics cache");
            await analyticsService.InvalidateAnalyticsCacheAsync(ct);

            // Step 2: Get popular champions to pre-warm
            var popularChampions = await GetPopularChampionsAsync(ct);
            logger.LogInformation("Pre-warming cache for {Count} popular champions", popularChampions.Count);

            // Step 3: Pre-warm tier lists (high value, relatively few combinations)
            foreach (var role in Roles)
            {
                foreach (var tier in PrimaryTiers)
                {
                    try
                    {
                        await analyticsService.GetTierListAsync(role, tier, ct);
                        logger.LogDebug("Pre-warmed tier list: {Role}/{Tier}", role, tier);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to pre-warm tier list {Role}/{Tier}", role, tier);
                    }
                }

                // Also pre-warm "all tiers" tier list per role
                try
                {
                    await analyticsService.GetTierListAsync(role, null, ct);
                    logger.LogDebug("Pre-warmed tier list: {Role}/all", role);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to pre-warm tier list {Role}/all", role);
                }
            }

            // Step 4: Pre-warm unified tier list
            try
            {
                await analyticsService.GetTierListAsync("ALL", null, ct);
                logger.LogDebug("Pre-warmed unified tier list");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to pre-warm unified tier list");
            }

            // Step 5: Pre-warm win rates, builds, matchups for top 20 champions per role
            var preWarmCount = 0;
            foreach (var role in Roles)
            {
                var roleChampions = popularChampions
                    .Where(c => c.Role == role)
                    .Take(20)
                    .ToList();

                foreach (var champ in roleChampions)
                {
                    try
                    {
                        await analyticsService.GetWinRatesAsync(
                            champ.ChampionId,
                            new ChampionAnalyticsFilter(Role: role),
                            ct);

                        await analyticsService.GetBuildsAsync(champ.ChampionId, role, null, ct);
                        await analyticsService.GetMatchupsAsync(champ.ChampionId, role, null, ct);

                        preWarmCount++;
                        logger.LogDebug("Pre-warmed analytics for champion {ChampId} in {Role}",
                            champ.ChampionId, role);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to pre-warm analytics for champion {ChampId} in {Role}",
                            champ.ChampionId, role);
                    }
                }
            }

            stopwatch.Stop();
            logger.LogInformation(
                "Analytics refresh complete. Pre-warmed {Count} champion/role combinations in {Elapsed}ms",
                preWarmCount,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Analytics refresh failed after {Elapsed}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<List<(int ChampionId, string Role, int Games)>> GetPopularChampionsAsync(
        CancellationToken ct)
    {
        var currentPatch = await db.Patches
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => p.Version)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrEmpty(currentPatch))
        {
            logger.LogWarning("No active patch found, skipping popular champion lookup");
            return new List<(int, string, int)>();
        }

        var popular = await db.MatchParticipants
            .AsNoTracking()
            .Where(mp => mp.Match.Patch == currentPatch
                      && mp.Match.Status == Data.Models.LoL.Match.FetchStatus.Success
                      && mp.TeamPosition != null)
            .GroupBy(mp => new { mp.ChampionId, mp.TeamPosition })
            .Select(g => new
            {
                g.Key.ChampionId,
                Role = g.Key.TeamPosition!,
                Games = g.Count()
            })
            .OrderByDescending(x => x.Games)
            .Take(100)
            .ToListAsync(ct);

        return popular.Select(p => (p.ChampionId, p.Role, p.Games)).ToList();
    }
}
