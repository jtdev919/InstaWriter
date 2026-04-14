using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class WorkflowTransitionTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<T> CreateAsync<T>(string url, object payload, CancellationToken ct)
    {
        var response = await _client.PostAsJsonAsync(url, payload, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }

    private async Task<HttpResponseMessage> TransitionAsync(string url, string status, CancellationToken ct) =>
        await _client.PostAsJsonAsync(url, new { Status = status }, ct);

    // --- ContentIdea transitions ---

    [Fact]
    public async ValueTask Idea_Captured_To_Planned_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "Transition test" }, ct);

        var response = await TransitionAsync($"/api/content/ideas/{idea.Id}/transition", "Planned", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<ContentIdea>(ct);
        Assert.Equal(ContentIdeaStatus.Planned, updated!.Status);
    }

    [Fact]
    public async ValueTask Idea_Captured_To_Published_Fails()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "Invalid transition" }, ct);

        var response = await TransitionAsync($"/api/content/ideas/{idea.Id}/transition", "Published", ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask Idea_FullLifecycle_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "Full lifecycle" }, ct);

        // Captured -> Planned -> InProgress -> Published -> Archived
        foreach (var status in new[] { "Planned", "InProgress", "Published", "Archived" })
        {
            var response = await TransitionAsync($"/api/content/ideas/{idea.Id}/transition", status, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async ValueTask Idea_InvalidStatusString_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "Bad status" }, ct);

        var response = await TransitionAsync($"/api/content/ideas/{idea.Id}/transition", "Nonexistent", ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- ContentDraft transitions ---

    [Fact]
    public async ValueTask Draft_To_AwaitingReview_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        // Use Medium risk to prevent auto-approval
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "For draft transition", RiskLevel = "Medium" }, ct);
        var draft = await CreateAsync<ContentDraft>("/api/content/drafts",
            new { ContentIdeaId = idea.Id, Caption = "Test caption" }, ct);

        var response = await TransitionAsync($"/api/content/drafts/{draft.Id}/transition", "AwaitingReview", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Re-fetch to get the latest status (orchestration may have modified it)
        var fetchResp = await _client.GetAsync($"/api/content/drafts/{draft.Id}", ct);
        var updated = await fetchResp.Content.ReadFromJsonAsync<ContentDraft>(ct);
        Assert.Equal(ContentDraftStatus.AwaitingReview, updated!.Status);
    }

    [Fact]
    public async ValueTask Draft_CannotSkipReview()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "Skip review test" }, ct);
        var draft = await CreateAsync<ContentDraft>("/api/content/drafts",
            new { ContentIdeaId = idea.Id, Caption = "Test" }, ct);

        // Draft -> Approved should fail (must go through AwaitingReview)
        var response = await TransitionAsync($"/api/content/drafts/{draft.Id}/transition", "Approved", ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask Draft_FullLifecycle_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        // Use Medium risk to prevent auto-approval and test manual lifecycle
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "Draft lifecycle", RiskLevel = "Medium" }, ct);
        var draft = await CreateAsync<ContentDraft>("/api/content/drafts",
            new { ContentIdeaId = idea.Id, Caption = "Lifecycle caption" }, ct);

        // Draft -> AwaitingReview -> Approved -> Published
        foreach (var status in new[] { "AwaitingReview", "Approved", "Published" })
        {
            var response = await TransitionAsync($"/api/content/drafts/{draft.Id}/transition", status, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async ValueTask Draft_RejectedCanRevise()
    {
        var ct = TestContext.Current.CancellationToken;
        // Use Medium risk to prevent auto-approval
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "Rejection test", RiskLevel = "Medium" }, ct);
        var draft = await CreateAsync<ContentDraft>("/api/content/drafts",
            new { ContentIdeaId = idea.Id, Caption = "To be rejected" }, ct);

        // Draft -> AwaitingReview -> Rejected -> Draft (revision)
        foreach (var status in new[] { "AwaitingReview", "Rejected", "Draft" })
        {
            var response = await TransitionAsync($"/api/content/drafts/{draft.Id}/transition", status, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    // --- PublishJob transitions ---

    [Fact]
    public async ValueTask Job_Pending_To_Scheduled_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "For job" }, ct);
        var draft = await CreateAsync<ContentDraft>("/api/content/drafts",
            new { ContentIdeaId = idea.Id, Caption = "Job caption" }, ct);
        var job = await CreateAsync<PublishJob>("/api/publish/jobs",
            new { ContentDraftId = draft.Id }, ct);

        var response = await TransitionAsync($"/api/publish/jobs/{job.Id}/transition", "Scheduled", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask Job_CannotPublishDirectlyFromPending()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "For bad job" }, ct);
        var draft = await CreateAsync<ContentDraft>("/api/content/drafts",
            new { ContentIdeaId = idea.Id, Caption = "Bad job caption" }, ct);
        var job = await CreateAsync<PublishJob>("/api/publish/jobs",
            new { ContentDraftId = draft.Id }, ct);

        var response = await TransitionAsync($"/api/publish/jobs/{job.Id}/transition", "Published", ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask Job_FailedCanRetry()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateAsync<ContentIdea>("/api/content/ideas", new { Title = "Retry test" }, ct);
        var draft = await CreateAsync<ContentDraft>("/api/content/drafts",
            new { ContentIdeaId = idea.Id, Caption = "Retry caption" }, ct);
        var job = await CreateAsync<PublishJob>("/api/publish/jobs",
            new { ContentDraftId = draft.Id }, ct);

        // Pending -> Scheduled -> Publishing -> Failed -> Pending (retry)
        foreach (var status in new[] { "Scheduled", "Publishing", "Failed", "Pending" })
        {
            var response = await TransitionAsync($"/api/publish/jobs/{job.Id}/transition", status, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    // --- TaskItem transitions ---

    [Fact]
    public async ValueTask Task_Pending_To_InProgress_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = await CreateAsync<TaskItem>("/api/tasks",
            new { Owner = "Joe", TaskType = "RecordReel" }, ct);

        var response = await TransitionAsync($"/api/tasks/{task.Id}/transition", "InProgress", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<TaskItem>(ct);
        Assert.Equal(TaskItemStatus.InProgress, updated!.Status);
    }

    [Fact]
    public async ValueTask Task_CannotCompleteFromPending()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = await CreateAsync<TaskItem>("/api/tasks",
            new { Owner = "Joe", TaskType = "Upload" }, ct);

        // Pending -> Completed should fail (must go through InProgress)
        var response = await TransitionAsync($"/api/tasks/{task.Id}/transition", "Completed", ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask Task_CompleteEndpoint_RequiresInProgress()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = await CreateAsync<TaskItem>("/api/tasks",
            new { Owner = "Joe", TaskType = "Approve" }, ct);

        // /complete from Pending should fail
        var failResponse = await _client.PostAsync($"/api/tasks/{task.Id}/complete", null, ct);
        Assert.Equal(HttpStatusCode.BadRequest, failResponse.StatusCode);

        // Transition to InProgress first, then /complete succeeds
        await TransitionAsync($"/api/tasks/{task.Id}/transition", "InProgress", ct);

        var okResponse = await _client.PostAsync($"/api/tasks/{task.Id}/complete", null, ct);
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
    }
}
