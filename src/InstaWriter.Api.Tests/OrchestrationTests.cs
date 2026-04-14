using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class OrchestrationTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask IdeaTransition_LogsWorkflowEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateIdea(ct);

        await _client.PostAsJsonAsync($"/api/content/ideas/{idea.Id}/transition", new { Status = "Planned" }, ct);

        var events = await _client.GetFromJsonAsync<List<WorkflowEvent>>($"/api/workflow-events/by-entity/ContentIdea/{idea.Id}", ct);
        Assert.NotNull(events);
        Assert.Contains(events, e => e.EventType == "IdeaTransitioned");
    }

    [Fact]
    public async ValueTask IdeaToInProgress_WithoutBrief_LogsBriefNeeded()
    {
        var ct = TestContext.Current.CancellationToken;
        var idea = await CreateIdea(ct);

        await _client.PostAsJsonAsync($"/api/content/ideas/{idea.Id}/transition", new { Status = "Planned" }, ct);
        await _client.PostAsJsonAsync($"/api/content/ideas/{idea.Id}/transition", new { Status = "InProgress" }, ct);

        var events = await _client.GetFromJsonAsync<List<WorkflowEvent>>($"/api/workflow-events/by-entity/ContentIdea/{idea.Id}", ct);
        Assert.NotNull(events);
        Assert.Contains(events, e => e.EventType == "BriefGenerationNeeded");
    }

    [Fact]
    public async ValueTask DraftToAwaitingReview_CreatesApproval()
    {
        var ct = TestContext.Current.CancellationToken;
        // Use Medium risk idea to prevent auto-approval
        var draft = await CreateDraft(ct, ContentRiskLevel.Medium);

        await _client.PostAsJsonAsync($"/api/content/drafts/{draft.Id}/transition", new { Status = "AwaitingReview" }, ct);

        var approvals = await _client.GetFromJsonAsync<List<Approval>>($"/api/approvals/by-draft/{draft.Id}", ct);
        Assert.NotNull(approvals);
        Assert.Single(approvals);
        Assert.Equal(ApprovalDecision.Pending, approvals[0].Decision);
    }

    [Fact]
    public async ValueTask DraftToApproved_CreatesPublishJob()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = await CreateDraft(ct);

        // Move through: Draft -> AwaitingReview -> Approved
        await _client.PostAsJsonAsync($"/api/content/drafts/{draft.Id}/transition", new { Status = "AwaitingReview" }, ct);
        await _client.PostAsJsonAsync($"/api/content/drafts/{draft.Id}/transition", new { Status = "Approved" }, ct);

        // Verify a PublishJob was auto-created
        var jobs = await _client.GetFromJsonAsync<List<PublishJob>>("/api/publish/jobs", ct);
        Assert.NotNull(jobs);
        Assert.Contains(jobs, j => j.ContentDraftId == draft.Id && j.Status == PublishJobStatus.Pending);
    }

    [Fact]
    public async ValueTask DraftToApproved_DoesNotDuplicatePublishJob()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = await CreateDraft(ct);

        // Manually create a publish job first
        var manualJob = new PublishJob { ContentDraftId = draft.Id };
        await _client.PostAsJsonAsync("/api/publish/jobs", manualJob, ct);

        // Now approve the draft
        await _client.PostAsJsonAsync($"/api/content/drafts/{draft.Id}/transition", new { Status = "AwaitingReview" }, ct);
        await _client.PostAsJsonAsync($"/api/content/drafts/{draft.Id}/transition", new { Status = "Approved" }, ct);

        // Should only have one publish job for this draft
        var jobs = await _client.GetFromJsonAsync<List<PublishJob>>("/api/publish/jobs", ct);
        var draftJobs = jobs!.Where(j => j.ContentDraftId == draft.Id).ToList();
        Assert.Single(draftJobs);
    }

    [Fact]
    public async ValueTask PublishJobCancelled_CreatesManualPublishTask()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = await CreateDraft(ct);

        var job = new PublishJob { ContentDraftId = draft.Id };
        var jobResponse = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        var createdJob = (await jobResponse.Content.ReadFromJsonAsync<PublishJob>(ct))!;

        // Cancel the job
        await _client.PostAsJsonAsync($"/api/publish/jobs/{createdJob.Id}/transition", new { Status = "Cancelled" }, ct);

        // Verify a ManualPublish task was created
        var tasks = await _client.GetFromJsonAsync<List<TaskItem>>("/api/tasks", ct);
        Assert.NotNull(tasks);
        Assert.Contains(tasks, t => t.TaskType == "ManualPublish" && t.RelatedEntityId == createdJob.Id);
    }

    [Fact]
    public async ValueTask TaskTransition_LogsWorkflowEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = new TaskItem { TaskType = "Recording", Description = "Film a Reel" };
        var taskResponse = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        var created = (await taskResponse.Content.ReadFromJsonAsync<TaskItem>(ct))!;

        await _client.PostAsJsonAsync($"/api/tasks/{created.Id}/transition", new { Status = "InProgress" }, ct);

        var events = await _client.GetFromJsonAsync<List<WorkflowEvent>>($"/api/workflow-events/by-entity/TaskItem/{created.Id}", ct);
        Assert.NotNull(events);
        Assert.Contains(events, e => e.EventType == "TaskTransitioned");
    }

    [Fact]
    public async ValueTask TaskOverdue_LogsOverdueEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = new TaskItem { TaskType = "Recording", Description = "Film a Reel" };
        var taskResponse = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        var created = (await taskResponse.Content.ReadFromJsonAsync<TaskItem>(ct))!;

        await _client.PostAsJsonAsync($"/api/tasks/{created.Id}/transition", new { Status = "Overdue" }, ct);

        var events = await _client.GetFromJsonAsync<List<WorkflowEvent>>($"/api/workflow-events/by-entity/TaskItem/{created.Id}", ct);
        Assert.NotNull(events);
        Assert.Contains(events, e => e.EventType == "TaskOverdue");
    }

    [Fact]
    public async ValueTask TaskComplete_ViaShortcut_LogsWorkflowEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = new TaskItem { TaskType = "Recording", Description = "Film a Reel" };
        var taskResponse = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        var created = (await taskResponse.Content.ReadFromJsonAsync<TaskItem>(ct))!;

        // Move to InProgress first, then complete via shortcut
        await _client.PostAsJsonAsync($"/api/tasks/{created.Id}/transition", new { Status = "InProgress" }, ct);
        await _client.PostAsync($"/api/tasks/{created.Id}/complete", null, ct);

        var events = await _client.GetFromJsonAsync<List<WorkflowEvent>>($"/api/workflow-events/by-entity/TaskItem/{created.Id}", ct);
        Assert.NotNull(events);
        Assert.True(events.Count >= 2); // InProgress + Completed transitions
    }

    private async Task<ContentIdea> CreateIdea(CancellationToken ct)
    {
        var idea = new ContentIdea { Title = "Orchestration Test Idea", Summary = "Test", RiskLevel = ContentRiskLevel.Low };
        var response = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        return (await response.Content.ReadFromJsonAsync<ContentIdea>(ct))!;
    }

    private async Task<ContentDraft> CreateDraft(CancellationToken ct, ContentRiskLevel riskLevel = ContentRiskLevel.Low)
    {
        var idea = new ContentIdea { Title = "Orchestration Test Idea", Summary = "Test", RiskLevel = riskLevel };
        var ideaResponse = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResponse.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft { ContentIdeaId = createdIdea.Id, Caption = "Test orchestration caption for validation" };
        var response = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        return (await response.Content.ReadFromJsonAsync<ContentDraft>(ct))!;
    }
}
