using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class InsightSnapshotTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetInsightSnapshots_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/insights", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask CreateInsightSnapshot_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var job = await CreateTestPublishJob(ct);

        var snapshot = new InsightSnapshot
        {
            PublishJobId = job.Id,
            Reach = 1500,
            Views = 3200,
            Likes = 120,
            Comments = 15,
            Shares = 8,
            Saves = 45,
            ProfileVisits = 30,
            FollowsAttributed = 5
        };

        var response = await _client.PostAsJsonAsync("/api/insights", snapshot, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<InsightSnapshot>(ct);
        Assert.NotNull(created);
        Assert.Equal(1500, created.Reach);
        Assert.Equal(120, created.Likes);
        Assert.Equal(job.Id, created.PublishJobId);
    }

    [Fact]
    public async ValueTask CreateInsightSnapshot_InvalidJob_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var snapshot = new InsightSnapshot { PublishJobId = Guid.NewGuid(), Reach = 100 };

        var response = await _client.PostAsJsonAsync("/api/insights", snapshot, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask CreateThenGet_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestSnapshot(ct);

        var response = await _client.GetAsync($"/api/insights/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<InsightSnapshot>(ct);
        Assert.Equal(created.Reach, fetched!.Reach);
    }

    [Fact]
    public async ValueTask GetById_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/insights/{Guid.NewGuid()}", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetByJob_ReturnsMatchingSnapshots()
    {
        var ct = TestContext.Current.CancellationToken;
        var job = await CreateTestPublishJob(ct);

        var s1 = new InsightSnapshot { PublishJobId = job.Id, Reach = 100, Likes = 10 };
        var s2 = new InsightSnapshot { PublishJobId = job.Id, Reach = 200, Likes = 20 };
        await _client.PostAsJsonAsync("/api/insights", s1, ct);
        await _client.PostAsJsonAsync("/api/insights", s2, ct);

        var response = await _client.GetAsync($"/api/insights/by-job/{job.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var snapshots = await response.Content.ReadFromJsonAsync<List<InsightSnapshot>>(ct);
        Assert.NotNull(snapshots);
        Assert.True(snapshots.Count >= 2);
    }

    [Fact]
    public async ValueTask DeleteInsightSnapshot_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestSnapshot(ct);

        var deleteResponse = await _client.DeleteAsync($"/api/insights/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/insights/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<PublishJob> CreateTestPublishJob(CancellationToken ct)
    {
        var idea = new ContentIdea { Title = "Test Idea", Summary = "Summary", RiskLevel = ContentRiskLevel.Low };
        var ideaResponse = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResponse.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft { ContentIdeaId = createdIdea.Id, Caption = "Test caption" };
        var draftResponse = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResponse.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        var job = new PublishJob { ContentDraftId = createdDraft.Id, PlannedPublishDate = DateTime.UtcNow.AddDays(1) };
        var jobResponse = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        return (await jobResponse.Content.ReadFromJsonAsync<PublishJob>(ct))!;
    }

    private async Task<InsightSnapshot> CreateTestSnapshot(CancellationToken ct)
    {
        var job = await CreateTestPublishJob(ct);
        var snapshot = new InsightSnapshot { PublishJobId = job.Id, Reach = 500, Likes = 50 };
        var response = await _client.PostAsJsonAsync("/api/insights", snapshot, ct);
        return (await response.Content.ReadFromJsonAsync<InsightSnapshot>(ct))!;
    }
}
