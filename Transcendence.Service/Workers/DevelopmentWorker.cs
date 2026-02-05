using Hangfire;
using Transcendence.Service.Core.Services.Extensions;
using Transcendence.Service.Core.Services.Jobs;
using Transcendence.Service.Core.Services.StaticData.Implementations;

namespace Transcendence.Service.Workers;

public class DevelopmentWorker(IBackgroundJobClient backgroundJobClient, ILogger<DevelopmentWorker> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CleanupHangfireJobs();

        // Schedule patch detection every 6 hours
        RecurringJob.AddOrUpdate<UpdateStaticDataJob>(
            "detect-patch",
            job => job.Execute(CancellationToken.None),
            "0 */6 * * *"); // Every 6 hours at minute 0

        // Schedule analytics refresh daily at 4 AM UTC
        RecurringJob.AddOrUpdate<RefreshChampionAnalyticsJob>(
            "refresh-champion-analytics",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 4 * * *", // 4 AM UTC daily
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        // Schedule live game polling every minute. Job applies adaptive intervals per summoner state.
        RecurringJob.AddOrUpdate<LiveGamePollingJob>(
            "poll-live-games",
            job => job.ExecuteAsync(CancellationToken.None),
            "* * * * *",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        // Run immediately on startup for development
        BackgroundJob.Enqueue<UpdateStaticDataJob>(x => x.Execute(CancellationToken.None));

        return Task.CompletedTask;
    }

    private void CleanupHangfireJobs()
    {
        // clear any queued job or failed jobs
        JobStorage.Current?.GetMonitoringApi()?.PurgeJobs();
        RecurringJob.RemoveIfExists("*");
        logger.LogInformation("Cleared all jobs");
    }
}
