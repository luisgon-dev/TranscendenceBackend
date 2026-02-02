using Microsoft.EntityFrameworkCore;
using Transcendence.Data;
using Transcendence.Data.Models.LoL.Match;
using Transcendence.Service.Core.Services.RiotApi.Interfaces;

namespace Transcendence.Service.Core.Services.Jobs;

public class RetryFailedMatchesJob(
    TranscendenceContext context,
    IMatchService matchService,
    ILogger<RetryFailedMatchesJob> logger)
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        // Find matches with TemporaryFailure that haven't been attempted in last 10 minutes
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        var failedMatches = await context.Matches
            .IgnoreQueryFilters() // Include PermanentlyUnfetchable for complete view
            .Where(m => m.Status == FetchStatus.TemporaryFailure && m.LastAttemptAt < cutoff)
            .Take(100) // Batch size to prevent overwhelming API
            .ToListAsync(cancellationToken);

        logger.LogInformation("Retrying {Count} failed matches", failedMatches.Count);

        foreach (var match in failedMatches)
        {
            await matchService.FetchMatchWithRetryAsync(match.MatchId!, "na1", cancellationToken);
        }
    }
}
