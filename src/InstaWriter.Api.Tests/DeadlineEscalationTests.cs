using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using InstaWriter.Infrastructure.Orchestration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InstaWriter.Api.Tests;

public class DeadlineEscalationTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask OverdueTask_GetsTransitionedAndEscalated()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create a task with a past due date
        var task = new TaskItem
        {
            TaskType = "RecordReel",
            Description = "Record founder Reel",
            Owner = "joe",
            DueDate = DateTime.UtcNow.AddHours(-2)
        };
        var taskResp = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        var created = (await taskResp.Content.ReadFromJsonAsync<TaskItem>(ct))!;

        // Run escalation
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new DeadlineEscalationBackgroundService(scopeFactory, NullLogger<DeadlineEscalationBackgroundService>.Instance);
        await bgService.EscalateOverdueItemsAsync(ct);

        // Verify task is now Overdue
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.TaskItems.FindAsync(created.Id);
        Assert.Equal(TaskItemStatus.Overdue, updated!.Status);

        // Verify an escalation task was created
        var escalation = await db.TaskItems
            .FirstOrDefaultAsync(t => t.RelatedEntityType == "TaskItem"
                && t.RelatedEntityId == created.Id
                && t.TaskType == "EscalationReview", ct);
        Assert.NotNull(escalation);
        Assert.Equal(TaskPriority.Urgent, escalation.Priority);
        Assert.Equal("manager", escalation.Owner);
    }

    [Fact]
    public async ValueTask OverdueTask_DoesNotDuplicateEscalation()
    {
        var ct = TestContext.Current.CancellationToken;

        var task = new TaskItem
        {
            TaskType = "Upload",
            Description = "Upload media",
            Owner = "joe",
            DueDate = DateTime.UtcNow.AddHours(-3)
        };
        var taskResp = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        var created = (await taskResp.Content.ReadFromJsonAsync<TaskItem>(ct))!;

        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new DeadlineEscalationBackgroundService(scopeFactory, NullLogger<DeadlineEscalationBackgroundService>.Instance);

        // Run escalation twice
        await bgService.EscalateOverdueItemsAsync(ct);
        await bgService.EscalateOverdueItemsAsync(ct);

        // Should only have one escalation task
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var escalations = await db.TaskItems
            .Where(t => t.RelatedEntityType == "TaskItem"
                && t.RelatedEntityId == created.Id
                && t.TaskType == "EscalationReview")
            .CountAsync(ct);
        Assert.Equal(1, escalations);
    }

    [Fact]
    public async ValueTask StalledPublishJob_CreatesEscalation()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create idea -> draft -> publish job with a past planned date
        var idea = new ContentIdea { Title = "Stalled publish test", RiskLevel = ContentRiskLevel.Low };
        var ideaResp = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResp.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft { ContentIdeaId = createdIdea.Id, Caption = "Test" };
        var draftResp = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResp.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        var job = new PublishJob { ContentDraftId = createdDraft.Id, PlannedPublishDate = DateTime.UtcNow.AddDays(-2) };
        var jobResp = await _client.PostAsJsonAsync("/api/publish/jobs", job, ct);
        var createdJob = (await jobResp.Content.ReadFromJsonAsync<PublishJob>(ct))!;

        // Run escalation
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new DeadlineEscalationBackgroundService(scopeFactory, NullLogger<DeadlineEscalationBackgroundService>.Instance);
        await bgService.EscalateOverdueItemsAsync(ct);

        // Verify a publish escalation task was created
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var escalation = await db.TaskItems
            .FirstOrDefaultAsync(t => t.RelatedEntityType == "PublishJob"
                && t.RelatedEntityId == createdJob.Id
                && t.TaskType == "PublishEscalation", ct);
        Assert.NotNull(escalation);
        Assert.Equal(TaskPriority.Urgent, escalation.Priority);
    }

    [Fact]
    public async ValueTask StalledDraft_CreatesReviewEscalation()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create a draft and manually set it to AwaitingReview with an old CreatedAt
        var idea = new ContentIdea { Title = "Stalled review test", RiskLevel = ContentRiskLevel.Medium };
        var ideaResp = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResp.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft { ContentIdeaId = createdIdea.Id, Caption = "Needs review" };
        var draftResp = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResp.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        // Transition to AwaitingReview then backdate the CreatedAt
        await _client.PostAsJsonAsync($"/api/content/drafts/{createdDraft.Id}/transition", new { Status = "AwaitingReview" }, ct);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbDraft = await db.ContentDrafts.FindAsync(createdDraft.Id);
            dbDraft!.CreatedAt = DateTime.UtcNow.AddHours(-72); // 3 days old
            await db.SaveChangesAsync(ct);
        }

        // Run escalation
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new DeadlineEscalationBackgroundService(scopeFactory, NullLogger<DeadlineEscalationBackgroundService>.Instance);
        await bgService.EscalateOverdueItemsAsync(ct);

        // Verify a review escalation task was created
        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var escalation = await verifyDb.TaskItems
            .FirstOrDefaultAsync(t => t.RelatedEntityType == "ContentDraft"
                && t.RelatedEntityId == createdDraft.Id
                && t.TaskType == "ReviewEscalation", ct);
        Assert.NotNull(escalation);
        Assert.Equal(TaskPriority.High, escalation.Priority);
    }

    [Fact]
    public async ValueTask TaskWithFutureDueDate_NotEscalated()
    {
        var ct = TestContext.Current.CancellationToken;

        var task = new TaskItem
        {
            TaskType = "RecordReel",
            Description = "Future task",
            Owner = "joe",
            DueDate = DateTime.UtcNow.AddDays(5)
        };
        var taskResp = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        var created = (await taskResp.Content.ReadFromJsonAsync<TaskItem>(ct))!;

        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new DeadlineEscalationBackgroundService(scopeFactory, NullLogger<DeadlineEscalationBackgroundService>.Instance);
        await bgService.EscalateOverdueItemsAsync(ct);

        // Task should still be Pending
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unchanged = await db.TaskItems.FindAsync(created.Id);
        Assert.Equal(TaskItemStatus.Pending, unchanged!.Status);
    }
}
