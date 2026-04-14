using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Orchestration;

public class DeadlineEscalationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<DeadlineEscalationBackgroundService> logger) : BackgroundService
{
    // Check every hour
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Deadline escalation background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EscalateOverdueItemsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during deadline escalation cycle");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    public async Task EscalateOverdueItemsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orchestration = scope.ServiceProvider.GetRequiredService<IOrchestrationService>();

        var now = DateTime.UtcNow;

        // 1. Find tasks that are past their due date but still Pending or InProgress
        var overdueTasks = await db.TaskItems
            .Where(t => t.DueDate != null
                && t.DueDate < now
                && (t.Status == TaskItemStatus.Pending || t.Status == TaskItemStatus.InProgress))
            .ToListAsync(ct);

        foreach (var task in overdueTasks)
        {
            var fromStatus = task.Status;
            task.Status = TaskItemStatus.Overdue;
            await db.SaveChangesAsync(ct);

            logger.LogWarning("Task {TaskId} ({TaskType}) is overdue. Due: {DueDate}, Owner: {Owner}",
                task.Id, task.TaskType, task.DueDate, task.Owner);

            await orchestration.OnTaskItemTransitionAsync(task, fromStatus);

            // Create an escalation task for the manager if one doesn't already exist
            var hasEscalation = await db.TaskItems
                .AnyAsync(t => t.RelatedEntityType == "TaskItem"
                    && t.RelatedEntityId == task.Id
                    && t.TaskType == "EscalationReview", ct);

            if (!hasEscalation)
            {
                var escalationTask = new TaskItem
                {
                    Id = Guid.NewGuid(),
                    RelatedEntityType = "TaskItem",
                    RelatedEntityId = task.Id,
                    Owner = "manager",
                    TaskType = "EscalationReview",
                    Priority = TaskPriority.Urgent,
                    Description = $"Task '{task.TaskType}' (owned by {task.Owner}) is overdue since {task.DueDate:g}. Review and decide: reassign, extend deadline, or cancel.",
                    DueDate = now.AddHours(4),
                    Status = TaskItemStatus.Pending,
                    CreatedAt = now
                };
                db.TaskItems.Add(escalationTask);
                await db.SaveChangesAsync(ct);

                logger.LogInformation("Created escalation task {EscalationId} for overdue task {TaskId}",
                    escalationTask.Id, task.Id);
            }
        }

        // 2. Find publish jobs past their planned publish date that are still Pending
        var stalledJobs = await db.PublishJobs
            .Where(j => j.PlannedPublishDate != null
                && j.PlannedPublishDate < now.AddHours(-24)
                && j.Status == PublishJobStatus.Pending)
            .ToListAsync(ct);

        foreach (var job in stalledJobs)
        {
            var hasEscalation = await db.TaskItems
                .AnyAsync(t => t.RelatedEntityType == "PublishJob"
                    && t.RelatedEntityId == job.Id
                    && t.TaskType == "PublishEscalation", ct);

            if (!hasEscalation)
            {
                var escalationTask = new TaskItem
                {
                    Id = Guid.NewGuid(),
                    RelatedEntityType = "PublishJob",
                    RelatedEntityId = job.Id,
                    Owner = "manager",
                    TaskType = "PublishEscalation",
                    Priority = TaskPriority.Urgent,
                    Description = $"Publish job {job.Id} was planned for {job.PlannedPublishDate:g} but is still Pending. Decide: publish now, reschedule, or cancel.",
                    DueDate = now.AddHours(4),
                    Status = TaskItemStatus.Pending,
                    CreatedAt = now
                };
                db.TaskItems.Add(escalationTask);
                await db.SaveChangesAsync(ct);

                logger.LogWarning("Created publish escalation task {EscalationId} for stalled PublishJob {JobId}",
                    escalationTask.Id, job.Id);
            }
        }

        // 3. Find drafts stuck in AwaitingReview for more than 48 hours
        var stalledDrafts = await db.ContentDrafts
            .Where(d => d.Status == ContentDraftStatus.AwaitingReview
                && d.CreatedAt < now.AddHours(-48))
            .ToListAsync(ct);

        foreach (var draft in stalledDrafts)
        {
            var hasEscalation = await db.TaskItems
                .AnyAsync(t => t.RelatedEntityType == "ContentDraft"
                    && t.RelatedEntityId == draft.Id
                    && t.TaskType == "ReviewEscalation", ct);

            if (!hasEscalation)
            {
                var escalationTask = new TaskItem
                {
                    Id = Guid.NewGuid(),
                    RelatedEntityType = "ContentDraft",
                    RelatedEntityId = draft.Id,
                    Owner = "manager",
                    TaskType = "ReviewEscalation",
                    Priority = TaskPriority.High,
                    Description = $"Draft {draft.Id} has been awaiting review for over 48 hours. Please assign a reviewer or escalate.",
                    DueDate = now.AddHours(4),
                    Status = TaskItemStatus.Pending,
                    CreatedAt = now
                };
                db.TaskItems.Add(escalationTask);
                await db.SaveChangesAsync(ct);

                logger.LogWarning("Created review escalation task {EscalationId} for stalled draft {DraftId}",
                    escalationTask.Id, draft.Id);
            }
        }

        if (overdueTasks.Count > 0 || stalledJobs.Count > 0 || stalledDrafts.Count > 0)
        {
            logger.LogInformation("Escalation cycle complete: {OverdueTasks} overdue tasks, {StalledJobs} stalled jobs, {StalledDrafts} stalled drafts",
                overdueTasks.Count, stalledJobs.Count, stalledDrafts.Count);
        }
    }
}
