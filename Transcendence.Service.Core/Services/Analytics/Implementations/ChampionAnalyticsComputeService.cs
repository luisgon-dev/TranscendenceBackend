using Microsoft.EntityFrameworkCore;
using Transcendence.Data;
using Transcendence.Data.Models.LoL.Match;
using Transcendence.Service.Core.Services.Analytics.Interfaces;
using Transcendence.Service.Core.Services.Analytics.Models;

namespace Transcendence.Service.Core.Services.Analytics.Implementations;

/// <summary>
/// Raw computation service for champion analytics using EF Core aggregation.
/// </summary>
public class ChampionAnalyticsComputeService : IChampionAnalyticsComputeService
{
    private const int MinimumGamesRequired = 100;
    private readonly TranscendenceContext _context;

    public ChampionAnalyticsComputeService(TranscendenceContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Computes win rates for a champion across roles and rank tiers.
    /// Only returns data for combinations with 100+ games.
    /// </summary>
    public async Task<List<ChampionWinRateDto>> ComputeWinRatesAsync(
        int championId,
        ChampionAnalyticsFilter filter,
        string patch,
        CancellationToken ct)
    {
        // Base query: Match participants for this champion in this patch
        var query = _context.MatchParticipants
            .AsNoTracking()
            .Include(mp => mp.Match)
            .Include(mp => mp.Summoner)
                .ThenInclude(s => s.Ranks)
            .Where(mp => mp.ChampionId == championId)
            .Where(mp => mp.Match.Patch == patch)
            .Where(mp => mp.Match.Status == FetchStatus.Success)
            .Where(mp => mp.TeamPosition != null && mp.TeamPosition != "");

        // Apply region filter if specified
        if (!string.IsNullOrEmpty(filter.Region))
        {
            query = query.Where(mp => mp.Summoner.Region == filter.Region);
        }

        // Apply role filter if specified
        if (!string.IsNullOrEmpty(filter.Role))
        {
            query = query.Where(mp => mp.TeamPosition == filter.Role);
        }

        // Get all participants with their rank data
        var participants = await query.ToListAsync(ct);

        // For each participant, get their current rank tier at the time of the match
        // We'll use the most recent rank data (RANKED_SOLO_5x5 queue)
        var participantRanks = participants
            .Select(mp => new
            {
                Participant = mp,
                RankTier = mp.Summoner.Ranks
                    .Where(r => r.QueueType == "RANKED_SOLO_5x5")
                    .OrderByDescending(r => r.UpdatedAt)
                    .FirstOrDefault()?.Tier ?? "UNRANKED"
            })
            .ToList();

        // Apply rank tier filter if specified
        if (!string.IsNullOrEmpty(filter.RankTier))
        {
            participantRanks = participantRanks
                .Where(pr => pr.RankTier.Equals(filter.RankTier, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Group by role and rank tier, calculate win rates
        var winRateData = participantRanks
            .GroupBy(pr => new { pr.Participant.TeamPosition, pr.RankTier })
            .Select(g => new
            {
                Role = g.Key.TeamPosition!,
                RankTier = g.Key.RankTier,
                Games = g.Count(),
                Wins = g.Count(pr => pr.Participant.Win)
            })
            .Where(x => x.Games >= MinimumGamesRequired)
            .ToList();

        // Calculate total games across all roles/tiers for pick rate calculation
        var totalGames = participantRanks.Count;

        // Convert to DTOs
        var result = winRateData
            .Select(data => new ChampionWinRateDto(
                ChampionId: championId,
                Role: data.Role,
                RankTier: data.RankTier,
                Games: data.Games,
                Wins: data.Wins,
                WinRate: data.Games > 0 ? (double)data.Wins / data.Games : 0.0,
                PickRate: totalGames > 0 ? (double)data.Games / totalGames : 0.0,
                Patch: patch
            ))
            .OrderByDescending(x => x.Games)
            .ToList();

        return result;
    }
}
