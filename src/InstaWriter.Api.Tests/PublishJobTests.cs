using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class PublishJobTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<ContentDraft> CreateDraftAsync(CancellationToken ct)
    {
        var idea = new ContentIdea { Title = "Idea for publish job" };
        var ideaResponse = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResponse.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft
        {
            ContentIdeaId = createdIdea.Id,
            Caption = "Draft for publish job"
        };
        var draftResponse = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        return (await draftResponse.Content.ReadFromJsonAsync<ContentDraft>(ct))!;
    }

    [Fact]
    public async ValueTask PostJob_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = await CreateDraftAsync(ct);

        var job = new PublishJob
        {
            ContentDraftId = draft.Id,
            PlannedPublishDate = DateTime.UtcNow.AddDays(1)
        };

        var response = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<PublishJob>(ct);
        Assert.NotNull(created);
        Assert.Equal(PublishJobStatus.Pending, created.Status);
    }

    [Fact]
    public async ValueTask PostJob_InvalidDraft_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var job = new PublishJob { ContentDraftId = Guid.NewGuid() };

        var response = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetJobStatus_ReturnsStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = await CreateDraftAsync(ct);

        var job = new PublishJob
        {
            ContentDraftId = draft.Id,
            PlannedPublishDate = DateTime.UtcNow.AddDays(2)
        };

        var postResponse = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        var created = await postResponse.Content.ReadFromJsonAsync<PublishJob>(ct);

        var statusResponse = await _client.GetAsync($"/api/publish/jobs/{created!.Id}/status", ct);
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
    }
}
