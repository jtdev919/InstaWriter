using InstaWriter.Core.Entities;

namespace InstaWriter.Core.Services;

public interface IOrchestrationService
{
    Task OnContentIdeaTransitionAsync(ContentIdea idea, ContentIdeaStatus fromStatus, string? correlationId = null);
    Task OnContentDraftTransitionAsync(ContentDraft draft, ContentDraftStatus fromStatus, string? correlationId = null);
    Task OnPublishJobTransitionAsync(PublishJob job, PublishJobStatus fromStatus, string? correlationId = null);
    Task OnTaskItemTransitionAsync(TaskItem task, TaskItemStatus fromStatus, string? correlationId = null);
}
