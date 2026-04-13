using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class ContentBriefTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetContentBriefs_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/content/briefs", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetContentBriefs_ReturnsList()
    {
        var ct = TestContext.Current.CancellationToken;
        var briefs = await _client.GetFromJsonAsync<List<ContentBrief>>("/api/content/briefs", ct);
        Assert.NotNull(briefs);
    }

    [Fact]
    public async ValueTask CreateContentBrief_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateTestIdea(ct);

        var brief = new ContentBrief
        {
            ContentIdeaId = idea.Id,
            TargetFormat = ContentFormat.Carousel,
            Objective = "Educate audience on morning routine benefits",
            Audience = "Health-conscious professionals aged 25-40",
            HookDirection = "Start with a surprising statistic about morning habits",
            KeyMessage = "A consistent morning routine improves focus by 30%",
            CTA = "Save this for your morning reset",
            RequiresOriginalMedia = false,
            RequiresManualApproval = false
        };

        var response = await _client.PostAsJsonAsync("/api/content/briefs", brief, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ContentBrief>(ct);
        Assert.NotNull(created);
        Assert.Equal(ContentFormat.Carousel, created.TargetFormat);
        Assert.Equal(idea.Id, created.ContentIdeaId);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async ValueTask CreateContentBrief_InvalidIdea_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var brief = new ContentBrief
        {
            ContentIdeaId = Guid.NewGuid(),
            Objective = "Test objective",
            KeyMessage = "Test message"
        };

        var response = await _client.PostAsJsonAsync("/api/content/briefs", brief, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask CreateThenGet_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestBrief(ct);

        var response = await _client.GetAsync($"/api/content/briefs/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<ContentBrief>(ct);
        Assert.Equal(created.Objective, fetched!.Objective);
        Assert.Equal(created.ContentIdeaId, fetched.ContentIdeaId);
    }

    [Fact]
    public async ValueTask GetById_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/content/briefs/{Guid.NewGuid()}", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetByIdea_ReturnsMatchingBriefs()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateTestIdea(ct);

        var brief1 = new ContentBrief
        {
            ContentIdeaId = idea.Id,
            TargetFormat = ContentFormat.Reel,
            Objective = "First brief",
            KeyMessage = "Message one"
        };
        var brief2 = new ContentBrief
        {
            ContentIdeaId = idea.Id,
            TargetFormat = ContentFormat.StaticImage,
            Objective = "Second brief",
            KeyMessage = "Message two"
        };

        await _client.PostAsJsonAsync("/api/content/briefs", brief1, ct);
        await _client.PostAsJsonAsync("/api/content/briefs", brief2, ct);

        var response = await _client.GetAsync($"/api/content/briefs/by-idea/{idea.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var briefs = await response.Content.ReadFromJsonAsync<List<ContentBrief>>(ct);
        Assert.NotNull(briefs);
        Assert.True(briefs.Count >= 2);
        Assert.All(briefs, b => Assert.Equal(idea.Id, b.ContentIdeaId));
    }

    [Fact]
    public async ValueTask UpdateContentBrief_ChangesFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestBrief(ct);

        created.Objective = "Updated objective";
        created.TargetFormat = ContentFormat.Video;
        created.RequiresManualApproval = true;

        var putResponse = await _client.PutAsJsonAsync($"/api/content/briefs/{created.Id}", created, ct);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<ContentBrief>(ct);
        Assert.Equal("Updated objective", updated!.Objective);
        Assert.Equal(ContentFormat.Video, updated.TargetFormat);
        Assert.True(updated.RequiresManualApproval);
    }

    [Fact]
    public async ValueTask DeleteContentBrief_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestBrief(ct);

        var deleteResponse = await _client.DeleteAsync($"/api/content/briefs/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/content/briefs/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<ContentIdea> CreateTestIdea(CancellationToken ct)
    {
        var idea = new ContentIdea
        {
            Title = "Test Idea for Brief",
            Summary = "A test content idea",
            RiskLevel = ContentRiskLevel.Low
        };

        var response = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        return (await response.Content.ReadFromJsonAsync<ContentIdea>(ct))!;
    }

    private async Task<ContentBrief> CreateTestBrief(CancellationToken ct)
    {
        var idea = await CreateTestIdea(ct);

        var brief = new ContentBrief
        {
            ContentIdeaId = idea.Id,
            TargetFormat = ContentFormat.Carousel,
            Objective = "Test objective",
            Audience = "Test audience",
            HookDirection = "Test hook",
            KeyMessage = "Test key message",
            CTA = "Test CTA",
            RequiresOriginalMedia = false,
            RequiresManualApproval = false
        };

        var response = await _client.PostAsJsonAsync("/api/content/briefs", brief, ct);
        return (await response.Content.ReadFromJsonAsync<ContentBrief>(ct))!;
    }
}
