using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Orchestration;

public class OrchestrationService(AppDbContext db, IComplianceScorer complianceScorer, ILogger<OrchestrationService> logger) : IOrchestrationService
{
    public async Task OnContentIdeaTransitionAsync(ContentIdea idea, ContentIdeaStatus fromStatus, string? correlationId = null)
    {
        await LogWorkflowEventAsync("IdeaTransitioned", "ContentIdea", idea.Id, new { from = fromStatus.ToString(), to = idea.Status.ToString() }, correlationId);

        if (idea.Status == ContentIdeaStatus.InProgress)
        {
            // Check if a brief exists for this idea; if not, log that one is needed
            var hasBrief = await db.ContentBriefs.AnyAsync(b => b.ContentIdeaId == idea.Id);
            if (!hasBrief)
            {
                logger.LogInformation("Idea {IdeaId} moved to InProgress but has no ContentBrief — brief generation needed", idea.Id);
                await LogWorkflowEventAsync("BriefGenerationNeeded", "ContentIdea", idea.Id, new { ideaId = idea.Id }, correlationId);
            }
        }
    }

    public async Task OnContentDraftTransitionAsync(ContentDraft draft, ContentDraftStatus fromStatus, string? correlationId = null)
    {
        await LogWorkflowEventAsync("DraftTransitioned", "ContentDraft", draft.Id, new { from = fromStatus.ToString(), to = draft.Status.ToString() }, correlationId);

        switch (draft.Status)
        {
            case ContentDraftStatus.AwaitingReview:
                // Auto-score compliance
                var complianceResult = complianceScorer.ScoreContent(draft.Caption, draft.Script);
                draft.ComplianceScore = complianceResult.Score;
                await db.SaveChangesAsync();

                logger.LogInformation("Compliance scored Draft {DraftId}: score={Score}, risk={Risk}, flags={FlagCount}",
                    draft.Id, complianceResult.Score, complianceResult.RiskLevel, complianceResult.Flags.Length);
                await LogWorkflowEventAsync("ComplianceScored", "ContentDraft", draft.Id,
                    new { score = complianceResult.Score, riskLevel = complianceResult.RiskLevel, flags = complianceResult.Flags }, correlationId);

                // Determine the parent idea's risk level for threshold comparison
                var parentIdeaForRisk = await db.ContentIdeas.FindAsync(draft.ContentIdeaId);
                var ideaRisk = parentIdeaForRisk?.RiskLevel ?? ContentRiskLevel.Medium;

                // Auto-approve low-risk content with high compliance scores
                var canAutoApprove = ideaRisk == ContentRiskLevel.Low && complianceResult.Score >= 0.8;

                if (canAutoApprove)
                {
                    // Auto-approve: skip manual review
                    var autoApproval = new Approval
                    {
                        Id = Guid.NewGuid(),
                        ContentDraftId = draft.Id,
                        Approver = "system:auto-approve",
                        Decision = ApprovalDecision.Approved,
                        Comments = $"Auto-approved: compliance score {complianceResult.Score:F2}, risk level {complianceResult.RiskLevel}",
                        Timestamp = DateTime.UtcNow
                    };
                    db.Approvals.Add(autoApproval);

                    // Move draft directly to Approved
                    draft.Status = ContentDraftStatus.Approved;
                    await db.SaveChangesAsync();

                    logger.LogInformation("Auto-approved Draft {DraftId} (score={Score}, ideaRisk={Risk})",
                        draft.Id, complianceResult.Score, ideaRisk);
                    await LogWorkflowEventAsync("AutoApproved", "ContentDraft", draft.Id,
                        new { score = complianceResult.Score, ideaRisk = ideaRisk.ToString() }, correlationId);

                    // Trigger the Approved side effects (PublishJob creation)
                    await OnContentDraftTransitionAsync(draft, ContentDraftStatus.AwaitingReview, correlationId);
                }
                else
                {
                    // Create a pending Approval record for manual review
                    var approval = new Approval
                    {
                        Id = Guid.NewGuid(),
                        ContentDraftId = draft.Id,
                        Approver = "unassigned",
                        Decision = ApprovalDecision.Pending,
                        Comments = complianceResult.Flags.Length > 0
                            ? $"Compliance flags: {string.Join("; ", complianceResult.Flags)}"
                            : null,
                        Timestamp = DateTime.UtcNow
                    };
                    db.Approvals.Add(approval);
                    await db.SaveChangesAsync();

                    logger.LogInformation("Created pending Approval {ApprovalId} for Draft {DraftId} (score={Score}, risk={Risk})",
                        approval.Id, draft.Id, complianceResult.Score, complianceResult.RiskLevel);
                    await LogWorkflowEventAsync("ApprovalCreated", "Approval", approval.Id,
                        new { draftId = draft.Id, complianceScore = complianceResult.Score, riskLevel = complianceResult.RiskLevel }, correlationId);
                }
                break;

            case ContentDraftStatus.Approved:
                // Create a PublishJob if one doesn't already exist for this draft
                var hasJob = await db.PublishJobs.AnyAsync(j => j.ContentDraftId == draft.Id);
                if (!hasJob)
                {
                    // Pull the planned publish date from the parent idea if available
                    DateTime? plannedDate = null;
                    var idea = await db.ContentIdeas.FindAsync(draft.ContentIdeaId);
                    if (idea?.PlannedPublishDate != null)
                        plannedDate = idea.PlannedPublishDate;

                    var job = new PublishJob
                    {
                        Id = Guid.NewGuid(),
                        ContentDraftId = draft.Id,
                        PlannedPublishDate = plannedDate,
                        Status = PublishJobStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.PublishJobs.Add(job);
                    await db.SaveChangesAsync();

                    logger.LogInformation("Created PublishJob {JobId} for approved Draft {DraftId}", job.Id, draft.Id);
                    await LogWorkflowEventAsync("PublishJobCreated", "PublishJob", job.Id, new { draftId = draft.Id, plannedDate }, correlationId);
                }
                break;

            case ContentDraftStatus.Published:
                // Cascade Published status to the parent ContentIdea
                var parentIdea = await db.ContentIdeas.FindAsync(draft.ContentIdeaId);
                if (parentIdea != null && parentIdea.Status == ContentIdeaStatus.InProgress)
                {
                    parentIdea.Status = ContentIdeaStatus.Published;
                    await db.SaveChangesAsync();

                    logger.LogInformation("Cascaded Published status to ContentIdea {IdeaId}", parentIdea.Id);
                    await LogWorkflowEventAsync("IdeaTransitioned", "ContentIdea", parentIdea.Id, new { from = "InProgress", to = "Published", trigger = "DraftPublished" }, correlationId);
                }
                break;
        }
    }

    public async Task OnPublishJobTransitionAsync(PublishJob job, PublishJobStatus fromStatus, string? correlationId = null)
    {
        await LogWorkflowEventAsync("PublishJobTransitioned", "PublishJob", job.Id, new { from = fromStatus.ToString(), to = job.Status.ToString() }, correlationId);

        switch (job.Status)
        {
            case PublishJobStatus.Published:
                // Cascade Published status to the draft
                var draft = await db.ContentDrafts.FindAsync(job.ContentDraftId);
                if (draft != null && draft.Status == ContentDraftStatus.Approved)
                {
                    var draftFromStatus = draft.Status;
                    draft.Status = ContentDraftStatus.Published;
                    await db.SaveChangesAsync();

                    logger.LogInformation("Cascaded Published status to ContentDraft {DraftId}", draft.Id);
                    await OnContentDraftTransitionAsync(draft, draftFromStatus, correlationId);
                }
                break;

            case PublishJobStatus.Failed:
                logger.LogWarning("PublishJob {JobId} failed: {Reason}", job.Id, job.FailureReason);
                await LogWorkflowEventAsync("PublishFailed", "PublishJob", job.Id, new { reason = job.FailureReason }, correlationId);
                break;

            case PublishJobStatus.Cancelled:
                // Create a manual publish task as fallback
                if (fromStatus is PublishJobStatus.Failed or PublishJobStatus.Pending or PublishJobStatus.Scheduled)
                {
                    var manualTask = new TaskItem
                    {
                        Id = Guid.NewGuid(),
                        RelatedEntityType = "PublishJob",
                        RelatedEntityId = job.Id,
                        Owner = "unassigned",
                        TaskType = "ManualPublish",
                        Priority = TaskPriority.High,
                        Description = $"Publish job {job.Id} was cancelled. Please publish manually from the Instagram app.",
                        Status = TaskItemStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.TaskItems.Add(manualTask);
                    await db.SaveChangesAsync();

                    logger.LogInformation("Created manual publish TaskItem {TaskId} for cancelled PublishJob {JobId}", manualTask.Id, job.Id);
                    await LogWorkflowEventAsync("TaskCreated", "TaskItem", manualTask.Id, new { type = "ManualPublish", publishJobId = job.Id }, correlationId);
                }
                break;
        }
    }

    public async Task OnTaskItemTransitionAsync(TaskItem task, TaskItemStatus fromStatus, string? correlationId = null)
    {
        await LogWorkflowEventAsync("TaskTransitioned", "TaskItem", task.Id, new { from = fromStatus.ToString(), to = task.Status.ToString() }, correlationId);

        if (task.Status == TaskItemStatus.Overdue)
        {
            logger.LogWarning("TaskItem {TaskId} is now overdue (type: {TaskType}, owner: {Owner})", task.Id, task.TaskType, task.Owner);
            await LogWorkflowEventAsync("TaskOverdue", "TaskItem", task.Id, new { taskType = task.TaskType, owner = task.Owner }, correlationId);
        }

        if (task.Status == TaskItemStatus.Completed && task.RelatedEntityType == "PublishJob" && task.RelatedEntityId.HasValue)
        {
            // If a manual publish task is completed, check if we should mark the publish job
            logger.LogInformation("Manual task {TaskId} completed for PublishJob {JobId}", task.Id, task.RelatedEntityId);
            await LogWorkflowEventAsync("ManualPublishCompleted", "TaskItem", task.Id, new { publishJobId = task.RelatedEntityId }, correlationId);
        }
    }

    private async Task LogWorkflowEventAsync(string eventType, string entityType, Guid entityId, object? payload = null, string? correlationId = null)
    {
        var wfEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            EventTime = DateTime.UtcNow,
            PayloadJson = payload != null ? System.Text.Json.JsonSerializer.Serialize(payload) : null,
            CorrelationId = correlationId
        };

        db.WorkflowEvents.Add(wfEvent);
        await db.SaveChangesAsync();
    }
}
