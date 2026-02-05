using Microsoft.EntityFrameworkCore;
using Transcendence.Data;
using Transcendence.Data.Models.LiveGame;
using Transcendence.Data.Repositories.Interfaces;
using Transcendence.Service.Core.Services.LiveGame.Interfaces;
using Transcendence.Service.Core.Services.LiveGame.Models;

namespace Transcendence.Service.Core.Services.Jobs;

public class LiveGamePollingJob(
    TranscendenceContext db,
    ILiveGameService liveGameService,
    ILiveGameSnapshotRepository snapshotRepository,
    ILogger<LiveGamePollingJob> logger)
{
    private const int MaxTrackedSummonersPerRun = 200;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var trackedSummoners = await db.Summoners
            .AsNoTracking()
            .Where(s => s.Puuid != null && s.GameName != null && s.TagLine != null && s.PlatformRegion != null)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(MaxTrackedSummonersPerRun)
            .Select(s => new
            {
                s.Id,
                s.Puuid,
                s.GameName,
                s.TagLine,
                s.PlatformRegion
            })
            .ToListAsync(ct);

        var processed = 0;
        foreach (var summoner in trackedSummoners)
        {
            var latest = await snapshotRepository.GetLatestByPuuidAsync(
                summoner.Puuid!,
                summoner.PlatformRegion!,
                ct);

            if (latest != null && latest.NextPollAtUtc > now)
                continue;

            try
            {
                var response = await liveGameService.GetCurrentGameAsync(
                    summoner.PlatformRegion!,
                    summoner.GameName!,
                    summoner.TagLine!,
                    ct);

                var nextPollAt = now.Add(LiveGamePollingState.GetNextInterval(response.State));
                var snapshot = new LiveGameSnapshot
                {
                    Id = Guid.NewGuid(),
                    SummonerId = summoner.Id,
                    Puuid = summoner.Puuid!,
                    PlatformRegion = summoner.PlatformRegion!,
                    State = response.State,
                    GameId = response.GameId,
                    ObservedAtUtc = now,
                    NextPollAtUtc = nextPollAt
                };

                await snapshotRepository.AddAsync(snapshot, ct);
                await snapshotRepository.SaveChangesAsync(ct);
                processed++;

                if (latest?.State == "in_game" && response.State == "offline")
                {
                    logger.LogInformation(
                        "Live game ended for {Region}/{Puuid}, previous game {GameId}",
                        summoner.PlatformRegion,
                        summoner.Puuid,
                        latest.GameId);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Polling failed for {Region}/{GameName}#{TagLine}",
                    summoner.PlatformRegion,
                    summoner.GameName,
                    summoner.TagLine);
            }
        }

        logger.LogInformation("Live game polling cycle complete. Processed {Count} summoners.", processed);
    }
}
