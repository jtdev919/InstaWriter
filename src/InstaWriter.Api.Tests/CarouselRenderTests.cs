using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Carousel;
using Xunit;

namespace InstaWriter.Api.Tests;

public class CarouselRenderTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public void CompositionService_Generates8Slides()
    {
        var brief = new ContentBrief
        {
            Id = Guid.NewGuid(),
            ContentIdeaId = Guid.NewGuid(),
            TargetFormat = ContentFormat.Carousel,
            Objective = "Educate about morning routines",
            Audience = "Health-conscious professionals",
            HookDirection = "What if your morning routine is sabotaging your energy?",
            KeyMessage = "Consistent morning habits boost focus by 30%",
            CTA = "Save this for your morning reset"
        };

        var request = CarouselCompositionService.ComposeFromBrief(brief, "@wellness");

        Assert.Equal(8, request.Slides.Count);
        Assert.Equal("title", request.Slides[0].Layout);
        Assert.Equal("cta", request.Slides[7].Layout);
        Assert.Equal("@wellness", request.Author);
    }

    [Fact]
    public async ValueTask RenderCarousel_ReturnsAssets()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create idea + brief
        var idea = new ContentIdea { Title = "Carousel render test", RiskLevel = ContentRiskLevel.Low, PillarName = "wellness" };
        var ideaResp = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResp.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var brief = new ContentBrief
        {
            ContentIdeaId = createdIdea.Id,
            TargetFormat = ContentFormat.Carousel,
            Objective = "Test carousel rendering",
            KeyMessage = "This is a test key message",
            HookDirection = "Hook direction test",
            CTA = "Follow for more"
        };
        var briefResp = await _client.PostAsJsonAsync("/api/content/briefs", brief, ct);
        var createdBrief = (await briefResp.Content.ReadFromJsonAsync<ContentBrief>(ct))!;

        // Render carousel
        var renderResp = await _client.PostAsync($"/api/content/briefs/{createdBrief.Id}/render-carousel", null, ct);
        Assert.Equal(HttpStatusCode.OK, renderResp.StatusCode);

        var result = await renderResp.Content.ReadFromJsonAsync<RenderResult>(ct);
        Assert.NotNull(result);
        Assert.Equal(8, result.SlideCount);
        Assert.Equal(8, result.AssetIds.Count);

        // Verify assets were created
        var assetsResp = await _client.GetFromJsonAsync<List<Asset>>("/api/assets", ct);
        foreach (var assetId in result.AssetIds)
        {
            Assert.Contains(assetsResp!, a => a.Id.ToString() == assetId);
        }
    }

    [Fact]
    public async ValueTask RenderCarousel_NotFound_ForInvalidBrief()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.PostAsync($"/api/content/briefs/{Guid.NewGuid()}/render-carousel", null, ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask RenderCarousel_AssetsTaggedCorrectly()
    {
        var ct = TestContext.Current.CancellationToken;

        var idea = new ContentIdea { Title = "Tag test", RiskLevel = ContentRiskLevel.Low, PillarName = "fitness" };
        var ideaResp = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResp.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var brief = new ContentBrief
        {
            ContentIdeaId = createdIdea.Id,
            TargetFormat = ContentFormat.Carousel,
            Objective = "Tag test",
            KeyMessage = "Message",
            CTA = "CTA"
        };
        var briefResp = await _client.PostAsJsonAsync("/api/content/briefs", brief, ct);
        var createdBrief = (await briefResp.Content.ReadFromJsonAsync<ContentBrief>(ct))!;

        await _client.PostAsync($"/api/content/briefs/{createdBrief.Id}/render-carousel", null, ct);

        var assets = await _client.GetFromJsonAsync<List<Asset>>("/api/assets", ct);
        var carouselAssets = assets!.Where(a => a.Tags != null && a.Tags.Contains("carousel")).ToList();
        Assert.True(carouselAssets.Count >= 8);
        Assert.All(carouselAssets, a =>
        {
            Assert.Equal(AssetType.Carousel, a.AssetType);
            Assert.Equal(AssetStatus.Ready, a.Status);
            Assert.Equal("fitness", a.PillarName);
        });
    }

    private record RenderResult(string BriefId, int SlideCount, List<string> AssetIds, string Message);
}
