using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class ContentIdeasTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetIdeas_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/content/ideas", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetIdeas_ReturnsList()
    {
        var ct = TestContext.Current.CancellationToken;
        var ideas = await _client.GetFromJsonAsync<List<ContentIdea>>("/api/content/ideas", ct);

        Assert.NotNull(ideas);
    }

    [Fact]
    public async ValueTask PostIdea_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = new ContentIdea
        {
            Title = "Test idea",
            Summary = "Integration test",
            PillarName = "Founder journey"
        };

        var response = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ContentIdea>(ct);
        Assert.NotNull(created);
        Assert.Equal("Test idea", created.Title);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async ValueTask PostThenGet_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = new ContentIdea
        {
            Title = "Round trip test",
            Summary = "Should persist and retrieve",
            PillarName = "App build in public",
            RiskLevel = ContentRiskLevel.Medium
        };

        var postResponse = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var created = await postResponse.Content.ReadFromJsonAsync<ContentIdea>(ct);

        var getResponse = await _client.GetAsync($"/api/content/ideas/{created!.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ContentIdea>(ct);
        Assert.Equal("Round trip test", fetched!.Title);
        Assert.Equal(ContentRiskLevel.Medium, fetched.RiskLevel);
    }

    [Fact]
    public async ValueTask GetById_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/content/ideas/{Guid.NewGuid()}", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask DeleteIdea_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = new ContentIdea { Title = "To be deleted" };
        var postResponse = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var created = await postResponse.Content.ReadFromJsonAsync<ContentIdea>(ct);

        var deleteResponse = await _client.DeleteAsync($"/api/content/ideas/{created!.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/content/ideas/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
