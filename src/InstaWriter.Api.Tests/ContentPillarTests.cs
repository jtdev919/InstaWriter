using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class ContentPillarTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetContentPillars_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/content-pillars", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask CreateContentPillar_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var pillar = new ContentPillar
        {
            Name = "Health Education",
            Description = "Evidence-based health tips and biomarker explainers",
            PriorityWeight = 3.0
        };

        var response = await _client.PostAsJsonAsync("/api/content-pillars", pillar, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ContentPillar>(ct);
        Assert.NotNull(created);
        Assert.Equal("Health Education", created.Name);
        Assert.Equal(3.0, created.PriorityWeight);
    }

    [Fact]
    public async ValueTask CreateThenGet_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestPillar(ct);

        var response = await _client.GetAsync($"/api/content-pillars/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<ContentPillar>(ct);
        Assert.Equal(created.Name, fetched!.Name);
    }

    [Fact]
    public async ValueTask GetById_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/content-pillars/{Guid.NewGuid()}", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask UpdateContentPillar_ChangesFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestPillar(ct);

        created.Name = "Updated Pillar";
        created.PriorityWeight = 5.0;

        var putResponse = await _client.PutAsJsonAsync($"/api/content-pillars/{created.Id}", created, ct);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<ContentPillar>(ct);
        Assert.Equal("Updated Pillar", updated!.Name);
        Assert.Equal(5.0, updated.PriorityWeight);
    }

    [Fact]
    public async ValueTask DeleteContentPillar_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestPillar(ct);

        var deleteResponse = await _client.DeleteAsync($"/api/content-pillars/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/content-pillars/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<ContentPillar> CreateTestPillar(CancellationToken ct)
    {
        var pillar = new ContentPillar
        {
            Name = "Test Pillar",
            Description = "Test description",
            PriorityWeight = 1.0
        };
        var response = await _client.PostAsJsonAsync("/api/content-pillars", pillar, ct);
        return (await response.Content.ReadFromJsonAsync<ContentPillar>(ct))!;
    }
}
