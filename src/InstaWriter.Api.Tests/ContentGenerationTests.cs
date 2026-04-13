using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class ContentGenerationTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<ContentIdea> CreateIdeaAsync(string title, CancellationToken ct)
    {
        var idea = new ContentIdea
        {
            Title = title,
            Summary = "Test summary",
            PillarName = "Data-to-insight education"
        };
        var response = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        return (await response.Content.ReadFromJsonAsync<ContentIdea>(ct))!;
    }

    [Fact]
    public async ValueTask GenerateDraft_CreatesNewDraft()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateIdeaAsync("3 reasons your HRV may be low", ct);

        var request = new { ContentIdeaId = idea.Id, TargetFormat = "carousel" };
        var response = await _client.PostAsJsonAsync("/api/content/drafts/generate", request, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var draft = await response.Content.ReadFromJsonAsync<ContentDraft>(ct);
        Assert.NotNull(draft);
        Assert.Contains("AI-generated caption", draft.Caption);
        Assert.NotEmpty(draft.HashtagSet!);
        Assert.Equal(ContentDraftStatus.Draft, draft.Status);
        Assert.Equal(idea.Id, draft.ContentIdeaId);
    }

    [Fact]
    public async ValueTask GenerateDraft_ForReel_IncludesScript()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateIdeaAsync("Why I'm building this health app", ct);

        var request = new { ContentIdeaId = idea.Id, TargetFormat = "reel" };
        var response = await _client.PostAsJsonAsync("/api/content/drafts/generate", request, ct);

        var draft = await response.Content.ReadFromJsonAsync<ContentDraft>(ct);
        Assert.NotNull(draft!.Script);
        Assert.Contains("Hook", draft.Script);
    }

    [Fact]
    public async ValueTask GenerateDraft_InvalidIdea_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var request = new { ContentIdeaId = Guid.NewGuid(), TargetFormat = "single_image" };

        var response = await _client.PostAsJsonAsync("/api/content/drafts/generate", request, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask RegenerateCaption_UpdatesDraft()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateIdeaAsync("Caption regen test", ct);

        // Create a draft first
        var genRequest = new { ContentIdeaId = idea.Id, TargetFormat = "single_image" };
        var genResponse = await _client.PostAsJsonAsync("/api/content/drafts/generate", genRequest, ct);
        var draft = await genResponse.Content.ReadFromJsonAsync<ContentDraft>(ct);

        // Regenerate with direction
        var regenRequest = new { Direction = "shorter" };
        var regenResponse = await _client.PostAsJsonAsync($"/api/content/drafts/{draft!.Id}/regenerate-caption", regenRequest, ct);

        Assert.Equal(HttpStatusCode.OK, regenResponse.StatusCode);

        var updated = await regenResponse.Content.ReadFromJsonAsync<ContentDraft>(ct);
        Assert.Contains("Regenerated (shorter)", updated!.Caption);
        Assert.Equal(2, updated.VersionNo);
    }

    [Fact]
    public async ValueTask ScoreCompliance_LowRisk_ReturnsLow()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateIdeaAsync("Safe content test", ct);

        var genRequest = new { ContentIdeaId = idea.Id, TargetFormat = "single_image" };
        var genResponse = await _client.PostAsJsonAsync("/api/content/drafts/generate", genRequest, ct);
        var draft = await genResponse.Content.ReadFromJsonAsync<ContentDraft>(ct);

        var complianceResponse = await _client.PostAsync($"/api/content/drafts/{draft!.Id}/score-compliance", null, ct);
        Assert.Equal(HttpStatusCode.OK, complianceResponse.StatusCode);

        var json = await complianceResponse.Content.ReadAsStringAsync(ct);
        Assert.Contains("Low", json);
    }
}
