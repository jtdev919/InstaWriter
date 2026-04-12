using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class ContentDraftTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<ContentIdea> CreateIdeaAsync(CancellationToken ct)
    {
        var idea = new ContentIdea { Title = "Test idea for draft" };
        var response = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        return (await response.Content.ReadFromJsonAsync<ContentIdea>(ct))!;
    }

    [Fact]
    public async ValueTask PostDraft_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateIdeaAsync(ct);

        var draft = new ContentDraft
        {
            ContentIdeaId = idea.Id,
            Caption = "3 reasons your HRV may be low #health #fitness"
        };

        var response = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ContentDraft>(ct);
        Assert.NotNull(created);
        Assert.Equal(1, created.VersionNo);
        Assert.Equal(ContentDraftStatus.Draft, created.Status);
    }

    [Fact]
    public async ValueTask PostDraft_InvalidIdea_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = new ContentDraft
        {
            ContentIdeaId = Guid.NewGuid(),
            Caption = "Orphan draft"
        };

        var response = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetDraftById_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateIdeaAsync(ct);

        var draft = new ContentDraft
        {
            ContentIdeaId = idea.Id,
            Caption = "Round trip caption",
            HashtagSet = "#test #roundtrip"
        };

        var postResponse = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var created = await postResponse.Content.ReadFromJsonAsync<ContentDraft>(ct);

        var getResponse = await _client.GetAsync($"/api/content/drafts/{created!.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ContentDraft>(ct);
        Assert.Equal("Round trip caption", fetched!.Caption);
    }

    [Fact]
    public async ValueTask DeleteDraft_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateIdeaAsync(ct);

        var draft = new ContentDraft { ContentIdeaId = idea.Id, Caption = "To delete" };
        var postResponse = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var created = await postResponse.Content.ReadFromJsonAsync<ContentDraft>(ct);

        var deleteResponse = await _client.DeleteAsync($"/api/content/drafts/{created!.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/content/drafts/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
