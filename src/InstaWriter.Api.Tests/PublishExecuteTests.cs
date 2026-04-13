using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class PublishExecuteTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<ChannelAccount> CreateConnectedChannelAsync(CancellationToken ct)
    {
        var account = new ChannelAccount
        {
            AccountName = "publish_test_account",
            ExternalAccountId = "ig_user_123",
            AccessToken = "valid_test_token"
        };

        var response = await _client.PostAsJsonAsync("/api/channels", account, ct);
        return (await response.Content.ReadFromJsonAsync<ChannelAccount>(ct))!;
    }

    private async Task<PublishJob> CreateScheduledJobAsync(Guid channelId, CancellationToken ct)
    {
        // Create idea -> draft -> job -> transition to Scheduled
        var idea = new ContentIdea { Title = "Publish test idea" };
        var ideaResponse = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResponse.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft
        {
            ContentIdeaId = createdIdea.Id,
            Caption = "Check out this amazing content! #health #fitness"
        };
        var draftResponse = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResponse.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        var job = new PublishJob
        {
            ContentDraftId = createdDraft.Id,
            ChannelAccountId = channelId,
            PlannedPublishDate = DateTime.UtcNow.AddHours(1)
        };
        var jobResponse = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        var createdJob = (await jobResponse.Content.ReadFromJsonAsync<PublishJob>(ct))!;

        // Transition Pending -> Scheduled
        await _client.PostAsJsonAsync($"/api/publish/jobs/{createdJob.Id}/transition", new { Status = "Scheduled" }, ct);

        return createdJob;
    }

    [Fact]
    public async ValueTask Execute_ScheduledJob_PublishesSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;
        var channel = await CreateConnectedChannelAsync(ct);
        var job = await CreateScheduledJobAsync(channel.Id, ct);

        var executeRequest = new { ImageUrl = "https://example.com/image.jpg" };
        var response = await _client.PostAsJsonAsync($"/api/publish/jobs/{job.Id}/execute", executeRequest, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}: {json}");
        Assert.Contains("fake_media_123", json);
    }

    [Fact]
    public async ValueTask Execute_PendingJob_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var channel = await CreateConnectedChannelAsync(ct);

        var idea = new ContentIdea { Title = "Not scheduled" };
        var ideaResponse = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResponse.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft { ContentIdeaId = createdIdea.Id, Caption = "Test" };
        var draftResponse = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResponse.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        var job = new PublishJob { ContentDraftId = createdDraft.Id, ChannelAccountId = channel.Id };
        var jobResponse = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        var createdJob = (await jobResponse.Content.ReadFromJsonAsync<PublishJob>(ct))!;

        // Try to execute while still Pending (not Scheduled)
        var executeRequest = new { ImageUrl = "https://example.com/image.jpg" };
        var response = await _client.PostAsJsonAsync($"/api/publish/jobs/{createdJob.Id}/execute", executeRequest, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask Execute_WithoutMedia_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var channel = await CreateConnectedChannelAsync(ct);
        var job = await CreateScheduledJobAsync(channel.Id, ct);

        // No imageUrl or videoUrl
        var executeRequest = new { ImageUrl = (string?)null, VideoUrl = (string?)null };
        var response = await _client.PostAsJsonAsync($"/api/publish/jobs/{job.Id}/execute", executeRequest, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask Execute_WithoutChannel_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create job without channel account
        var idea = new ContentIdea { Title = "No channel" };
        var ideaResponse = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResponse.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft { ContentIdeaId = createdIdea.Id, Caption = "Test" };
        var draftResponse = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResponse.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        var job = new PublishJob { ContentDraftId = createdDraft.Id };
        var jobResponse = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        var createdJob = (await jobResponse.Content.ReadFromJsonAsync<PublishJob>(ct))!;

        // Transition to Scheduled
        await _client.PostAsJsonAsync($"/api/publish/jobs/{createdJob.Id}/transition", new { Status = "Scheduled" }, ct);

        var executeRequest = new { ImageUrl = "https://example.com/image.jpg" };
        var response = await _client.PostAsJsonAsync($"/api/publish/jobs/{createdJob.Id}/execute", executeRequest, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
