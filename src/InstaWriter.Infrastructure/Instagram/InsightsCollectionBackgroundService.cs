using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Instagram;

public class InsightsCollectionBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<InsightsCollectionBackgroundService> logger) : BackgroundService
{
    // Check every 2 hours
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(2);

    // Collect snapshots at these ages after publishing
    private static readonly TimeSpan[] SnapshotWindows = [
        TimeSpan.FromHours(24),
        TimeSpan.FromHours(72),
        TimeSpan.FromDays(7)
    ];

    // Tolerance window — if a post is within this range of a snapshot window, collect
    private static readonly TimeSpan Tolerance = TimeSpan.FromHours(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Insights collection background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectInsightsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during insights collection cycle");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    public async Task CollectInsightsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var insightsService = scope.ServiceProvider.GetRequiredService<IInsightsService>();

        // Find published jobs with ExternalMediaId that have a connected channel account
        var publishedJobs = await db.PublishJobs
            .Include(j => j.ChannelAccount)
            .Where(j => j.Status == PublishJobStatus.Published
                && j.ExternalMediaId != null
                && j.ChannelAccount != null
                && j.ChannelAccount.AuthStatus == AuthStatus.Connected
                && j.ChannelAccount.AccessToken != null)
            .ToListAsync(ct);

        if (publishedJobs.Count == 0)
        {
            logger.LogDebug("No published jobs with media IDs found for insights collection");
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var job in publishedJobs)
        {
            // Determine which snapshots are due
            var existingSnapshots = await db.InsightSnapshots
                .Where(s => s.PublishJobId == job.Id)
                .CountAsync(ct);

            // Find the next snapshot window this job is eligible for
            if (existingSnapshots >= SnapshotWindows.Length)
                continue; // All snapshots already collected

            var targetWindow = SnapshotWindows[existingSnapshots];
            var publishedAt = job.CreatedAt; // Approximate — we use CreatedAt as publish time
            var targetTime = publishedAt.Add(targetWindow);

            // Check if we're within the tolerance window
            if (now < targetTime.Subtract(Tolerance))
                continue; // Too early for next snapshot

            logger.LogInformation("Collecting snapshot #{SnapshotNum} for PublishJob {JobId} (media {MediaId})",
                existingSnapshots + 1, job.Id, job.ExternalMediaId);

            var result = await insightsService.FetchMediaInsightsAsync(
                job.ChannelAccount!.AccessToken!, job.ExternalMediaId!, ct);

            if (result.Success)
            {
                var snapshot = new InsightSnapshot
                {
                    Id = Guid.NewGuid(),
                    PublishJobId = job.Id,
                    SnapshotDate = now,
                    Reach = result.Reach,
                    Views = result.Views,
                    Likes = result.Likes,
                    Comments = result.Comments,
                    Shares = result.Shares,
                    Saves = result.Saves,
                    ProfileVisits = result.ProfileVisits,
                    FollowsAttributed = result.FollowsAttributed
                };

                db.InsightSnapshots.Add(snapshot);
                await db.SaveChangesAsync(ct);

                logger.LogInformation("Snapshot saved for PublishJob {JobId}: reach={Reach}, likes={Likes}",
                    job.Id, result.Reach, result.Likes);
            }
            else
            {
                logger.LogWarning("Failed to collect insights for PublishJob {JobId}: {Error}",
                    job.Id, result.Error);
            }
        }
    }
}
