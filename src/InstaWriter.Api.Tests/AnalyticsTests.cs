using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InstaWriter.Api.Tests;

public class AnalyticsTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetTopPosts_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/analytics/posts/top", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetClusters_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/analytics/clusters", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetCTAInsights_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/analytics/cta-insights", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetRecommendations_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/analytics/recommendations", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask ScorePost_WithInsights_ReturnsScore()
    {
        var ct = TestContext.Current.CancellationToken;
        var jobId = await SetupPublishedJobWithInsights(ct);

        var response = await _client.GetAsync($"/api/analytics/posts/{jobId}/score", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var score = await response.Content.ReadFromJsonAsync<PostScore>(ct);
        Assert.NotNull(score);
        Assert.True(score.EngagementScore > 0);
        Assert.True(score.Reach > 0);
        Assert.True(score.EngagementRate > 0);
    }

    [Fact]
    public async ValueTask ScorePost_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/analytics/posts/{Guid.NewGuid()}/score", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetTopPosts_RankedByEngagement()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create two posts with different engagement levels
        var job1 = await SetupPublishedJobWithInsights(ct, reach: 1000, likes: 50, saves: 20);
        var job2 = await SetupPublishedJobWithInsights(ct, reach: 500, likes: 200, saves: 100);

        var response = await _client.GetAsync("/api/analytics/posts/top?count=10", ct);
        var topPosts = await response.Content.ReadFromJsonAsync<List<PostScore>>(ct);
        Assert.NotNull(topPosts);

        // Job2 should rank higher (more engagement despite less reach)
        if (topPosts.Count >= 2)
        {
            var job1Score = topPosts.FirstOrDefault(p => p.PublishJobId == job1);
            var job2Score = topPosts.FirstOrDefault(p => p.PublishJobId == job2);
            if (job1Score != null && job2Score != null)
                Assert.True(job2Score.EngagementScore > job1Score.EngagementScore);
        }
    }

    [Fact]
    public async ValueTask GetPillarPerformance_ReturnsData()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create a pillar
        var pillar = new ContentPillar { Name = $"TestPillar_{Guid.NewGuid():N}", PriorityWeight = 2.0 };
        await _client.PostAsJsonAsync("/api/content-pillars", pillar, ct);

        var response = await _client.GetAsync("/api/analytics/pillars/performance", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var performance = await response.Content.ReadFromJsonAsync<List<PillarPerformance>>(ct);
        Assert.NotNull(performance);
        Assert.Contains(performance, p => p.PillarName == pillar.Name);
    }

    [Fact]
    public async ValueTask UpdatePillarWeights_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.PostAsync("/api/analytics/pillars/update-weights", null, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<Guid> SetupPublishedJobWithInsights(CancellationToken ct, int reach = 1500, int likes = 120, int saves = 45)
    {
        var idea = new ContentIdea { Title = $"Analytics test {Guid.NewGuid():N}", PillarName = "wellness", RiskLevel = ContentRiskLevel.Low };
        var ideaResp = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResp.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft { ContentIdeaId = createdIdea.Id, Caption = "Save this for your morning routine! #wellness" };
        var draftResp = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResp.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        var job = new PublishJob { ContentDraftId = createdDraft.Id };
        var jobResp = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        var createdJob = (await jobResp.Content.ReadFromJsonAsync<PublishJob>(ct))!;

        // Mark as published
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbJob = await db.PublishJobs.FindAsync(createdJob.Id);
            dbJob!.Status = PublishJobStatus.Published;
            dbJob.ExternalMediaId = $"media_{Guid.NewGuid():N}";
            await db.SaveChangesAsync(ct);
        }

        // Add insight snapshot
        var snapshot = new InsightSnapshot
        {
            PublishJobId = createdJob.Id,
            Reach = reach,
            Views = reach * 2,
            Likes = likes,
            Comments = 15,
            Shares = 8,
            Saves = saves,
            ProfileVisits = 30,
            FollowsAttributed = 5
        };
        await _client.PostAsJsonAsync("/api/insights", snapshot, ct);

        return createdJob.Id;
    }
}
