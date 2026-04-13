using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class BrandProfileTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetBrandProfiles_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/brand-profiles", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetBrandProfiles_ReturnsList()
    {
        var ct = TestContext.Current.CancellationToken;
        var profiles = await _client.GetFromJsonAsync<List<BrandProfile>>("/api/brand-profiles", ct);
        Assert.NotNull(profiles);
    }

    [Fact]
    public async ValueTask CreateBrandProfile_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var profile = new BrandProfile
        {
            Name = "InstaWriter Health",
            VoiceGuide = "Approachable, evidence-based, encouraging",
            ToneGuide = "Warm but professional — avoid hype language",
            CTAStyle = "Soft CTA: 'Learn more in bio' or 'Save this for later'",
            DisclaimerRules = "All biomarker content must include 'Consult your physician' disclaimer",
            DefaultHashtagSets = "#healthtips #wellness #evidencebased"
        };

        var response = await _client.PostAsJsonAsync("/api/brand-profiles", profile, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<BrandProfile>(ct);
        Assert.NotNull(created);
        Assert.Equal("InstaWriter Health", created.Name);
        Assert.Equal("Approachable, evidence-based, encouraging", created.VoiceGuide);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async ValueTask CreateThenGet_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestProfile(ct);

        var response = await _client.GetAsync($"/api/brand-profiles/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<BrandProfile>(ct);
        Assert.Equal(created.Name, fetched!.Name);
        Assert.Equal(created.VoiceGuide, fetched.VoiceGuide);
    }

    [Fact]
    public async ValueTask GetById_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/brand-profiles/{Guid.NewGuid()}", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask UpdateBrandProfile_ChangesFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestProfile(ct);

        created.Name = "Updated Brand";
        created.ToneGuide = "Casual and fun";
        created.DefaultHashtagSets = "#updated #newtags";

        var putResponse = await _client.PutAsJsonAsync($"/api/brand-profiles/{created.Id}", created, ct);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<BrandProfile>(ct);
        Assert.Equal("Updated Brand", updated!.Name);
        Assert.Equal("Casual and fun", updated.ToneGuide);
        Assert.Equal("#updated #newtags", updated.DefaultHashtagSets);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async ValueTask DeleteBrandProfile_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestProfile(ct);

        var deleteResponse = await _client.DeleteAsync($"/api/brand-profiles/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/brand-profiles/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<BrandProfile> CreateTestProfile(CancellationToken ct)
    {
        var profile = new BrandProfile
        {
            Name = "Test Brand",
            VoiceGuide = "Test voice guide",
            ToneGuide = "Test tone guide",
            CTAStyle = "Test CTA style",
            DisclaimerRules = "Test disclaimer rules",
            DefaultHashtagSets = "#test #brand"
        };

        var response = await _client.PostAsJsonAsync("/api/brand-profiles", profile, ct);
        return (await response.Content.ReadFromJsonAsync<BrandProfile>(ct))!;
    }
}
