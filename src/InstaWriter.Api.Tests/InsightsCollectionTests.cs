using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using InstaWriter.Infrastructure.Instagram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InstaWriter.Api.Tests;

public class InsightsCollectionTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask CollectInsights_CreatesSnapshotForPublishedJob()
    {
        var ct = TestContext.Current.CancellationToken;

        // Set up: create a published job with ExternalMediaId and a connected channel
        var jobId = await SetupPublishedJob(ct);

        // Run the collection logic
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new InsightsCollectionBackgroundService(scopeFactory, NullLogger<InsightsCollectionBackgroundService>.Instance);
        await bgService.CollectInsightsAsync(ct);

        // Verify a snapshot was created
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var snapshots = await db.InsightSnapshots.Where(s => s.PublishJobId == jobId).ToListAsync(ct);
        Assert.Single(snapshots);
        Assert.Equal(1500, snapshots[0].Reach);
        Assert.Equal(120, snapshots[0].Likes);
        Assert.Equal(45, snapshots[0].Saves);
    }

    [Fact]
    public async ValueTask CollectInsights_SkipsJobsWithoutMediaId()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create a pending job (no ExternalMediaId)
        var idea = new ContentIdea { Title = "No media test", RiskLevel = ContentRiskLevel.Low };
        var ideaResp = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResp.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft { ContentIdeaId = createdIdea.Id, Caption = "Test" };
        var draftResp = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResp.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        var job = new PublishJob { ContentDraftId = createdDraft.Id };
        var jobResp = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        var createdJob = (await jobResp.Content.ReadFromJsonAsync<PublishJob>(ct))!;

        // Run collection — should not create any snapshots for this job
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new InsightsCollectionBackgroundService(scopeFactory, NullLogger<InsightsCollectionBackgroundService>.Instance);
        await bgService.CollectInsightsAsync(ct);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var snapshots = await db.InsightSnapshots.Where(s => s.PublishJobId == createdJob.Id).ToListAsync(ct);
        Assert.Empty(snapshots);
    }

    [Fact]
    public async ValueTask CollectInsights_DoesNotExceedThreeSnapshots()
    {
        var ct = TestContext.Current.CancellationToken;

        var jobId = await SetupPublishedJob(ct);

        // Manually insert 3 snapshots (the max)
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            for (int i = 0; i < 3; i++)
            {
                db.InsightSnapshots.Add(new InsightSnapshot
                {
                    Id = Guid.NewGuid(),
                    PublishJobId = jobId,
                    SnapshotDate = DateTime.UtcNow.AddHours(-i),
                    Reach = 100 * (i + 1)
                });
            }
            await db.SaveChangesAsync(ct);
        }

        // Run collection — should not create a 4th snapshot
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new InsightsCollectionBackgroundService(scopeFactory, NullLogger<InsightsCollectionBackgroundService>.Instance);
        await bgService.CollectInsightsAsync(ct);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await verifyDb.InsightSnapshots.CountAsync(s => s.PublishJobId == jobId, ct);
        Assert.Equal(3, count);
    }

    private async Task<Guid> SetupPublishedJob(CancellationToken ct)
    {
        // Create idea -> draft -> channel -> job
        var idea = new ContentIdea { Title = "Insights Test", RiskLevel = ContentRiskLevel.Low };
        var ideaResp = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResp.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft { ContentIdeaId = createdIdea.Id, Caption = "Test insights caption" };
        var draftResp = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResp.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        var channel = new ChannelAccount
        {
            AccountName = "insights-test-channel",
            ExternalAccountId = $"ig_{Guid.NewGuid():N}",
            PlatformType = PlatformType.Instagram
        };
        var channelResp = await _client.PostAsJsonAsync("/api/channels", channel, ct);
        var createdChannel = (await channelResp.Content.ReadFromJsonAsync<ChannelAccount>(ct))!;

        // Set token on channel
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbChannel = await db.ChannelAccounts.FindAsync(createdChannel.Id);
            dbChannel!.AccessToken = "test_token";
            dbChannel.TokenExpiry = DateTime.UtcNow.AddDays(30);
            dbChannel.AuthStatus = AuthStatus.Connected;
            await db.SaveChangesAsync(ct);
        }

        var job = new PublishJob { ContentDraftId = createdDraft.Id, ChannelAccountId = createdChannel.Id };
        var jobResp = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        var createdJob = (await jobResp.Content.ReadFromJsonAsync<PublishJob>(ct))!;

        // Mark as published with an external media ID — set CreatedAt far enough back to trigger snapshot
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbJob = await db.PublishJobs.FindAsync(createdJob.Id);
            dbJob!.Status = PublishJobStatus.Published;
            dbJob.ExternalMediaId = "fake_media_for_insights";
            dbJob.CreatedAt = DateTime.UtcNow.AddHours(-22); // Just under 24h to trigger first snapshot
            await db.SaveChangesAsync(ct);
        }

        return createdJob.Id;
    }
}
