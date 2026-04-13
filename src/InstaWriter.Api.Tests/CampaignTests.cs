using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class CampaignTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetCampaigns_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/campaigns", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask CreateCampaign_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var campaign = new Campaign
        {
            Name = "Summer Wellness Push",
            Objective = "Increase engagement 20% over 6 weeks",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(42),
            AudienceSegment = "Health-conscious millennials",
            KPISet = "engagement_rate,follower_growth,saves"
        };

        var response = await _client.PostAsJsonAsync("/api/campaigns", campaign, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Campaign>(ct);
        Assert.NotNull(created);
        Assert.Equal("Summer Wellness Push", created.Name);
        Assert.Equal(CampaignStatus.Draft, created.Status);
    }

    [Fact]
    public async ValueTask CreateThenGet_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestCampaign(ct);

        var response = await _client.GetAsync($"/api/campaigns/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<Campaign>(ct);
        Assert.Equal(created.Name, fetched!.Name);
    }

    [Fact]
    public async ValueTask GetById_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/campaigns/{Guid.NewGuid()}", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask UpdateCampaign_ChangesFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestCampaign(ct);

        created.Name = "Updated Campaign";
        created.Status = CampaignStatus.Active;
        created.AudienceSegment = "Gen Z fitness enthusiasts";

        var putResponse = await _client.PutAsJsonAsync($"/api/campaigns/{created.Id}", created, ct);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<Campaign>(ct);
        Assert.Equal("Updated Campaign", updated!.Name);
        Assert.Equal(CampaignStatus.Active, updated.Status);
        Assert.Equal("Gen Z fitness enthusiasts", updated.AudienceSegment);
    }

    [Fact]
    public async ValueTask DeleteCampaign_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestCampaign(ct);

        var deleteResponse = await _client.DeleteAsync($"/api/campaigns/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/campaigns/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<Campaign> CreateTestCampaign(CancellationToken ct)
    {
        var campaign = new Campaign
        {
            Name = "Test Campaign",
            Objective = "Test objective",
            AudienceSegment = "Test audience"
        };
        var response = await _client.PostAsJsonAsync("/api/campaigns", campaign, ct);
        return (await response.Content.ReadFromJsonAsync<Campaign>(ct))!;
    }
}
