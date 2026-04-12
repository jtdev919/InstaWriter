using InstaWriter.Core.Entities;

namespace InstaWriter.Core.Workflow;

public static class StatusTransitions
{
    // ContentIdea: Captured -> Planned -> InProgress -> Published -> Archived
    //                                                \-> Rejected -> Archived
    private static readonly Dictionary<ContentIdeaStatus, ContentIdeaStatus[]> _ideaTransitions = new()
    {
        [ContentIdeaStatus.Captured]   = [ContentIdeaStatus.Planned, ContentIdeaStatus.Rejected],
        [ContentIdeaStatus.Planned]    = [ContentIdeaStatus.InProgress, ContentIdeaStatus.Rejected],
        [ContentIdeaStatus.InProgress] = [ContentIdeaStatus.Published, ContentIdeaStatus.Rejected],
        [ContentIdeaStatus.Published]  = [ContentIdeaStatus.Archived],
        [ContentIdeaStatus.Rejected]   = [ContentIdeaStatus.Archived, ContentIdeaStatus.Captured],
        [ContentIdeaStatus.Archived]   = [],
    };

    // ContentDraft: Draft -> AwaitingReview -> Approved -> Published
    //                                      \-> Rejected -> Draft (revision)
    private static readonly Dictionary<ContentDraftStatus, ContentDraftStatus[]> _draftTransitions = new()
    {
        [ContentDraftStatus.Draft]          = [ContentDraftStatus.AwaitingReview],
        [ContentDraftStatus.AwaitingReview] = [ContentDraftStatus.Approved, ContentDraftStatus.Rejected],
        [ContentDraftStatus.Approved]       = [ContentDraftStatus.Published],
        [ContentDraftStatus.Rejected]       = [ContentDraftStatus.Draft],
        [ContentDraftStatus.Published]      = [],
    };

    // PublishJob: Pending -> Scheduled -> Publishing -> Published
    //                                              \-> Failed -> Pending (retry)
    //            Any non-terminal -> Cancelled
    private static readonly Dictionary<PublishJobStatus, PublishJobStatus[]> _publishTransitions = new()
    {
        [PublishJobStatus.Pending]    = [PublishJobStatus.Scheduled, PublishJobStatus.Cancelled],
        [PublishJobStatus.Scheduled]  = [PublishJobStatus.Publishing, PublishJobStatus.Cancelled],
        [PublishJobStatus.Publishing] = [PublishJobStatus.Published, PublishJobStatus.Failed],
        [PublishJobStatus.Published]  = [],
        [PublishJobStatus.Failed]     = [PublishJobStatus.Pending, PublishJobStatus.Cancelled],
        [PublishJobStatus.Cancelled]  = [],
    };

    // TaskItem: Pending -> InProgress -> Completed
    //                                \-> Cancelled
    //           Pending -> Overdue -> InProgress / Cancelled
    private static readonly Dictionary<TaskItemStatus, TaskItemStatus[]> _taskTransitions = new()
    {
        [TaskItemStatus.Pending]    = [TaskItemStatus.InProgress, TaskItemStatus.Cancelled, TaskItemStatus.Overdue],
        [TaskItemStatus.InProgress] = [TaskItemStatus.Completed, TaskItemStatus.Cancelled],
        [TaskItemStatus.Overdue]    = [TaskItemStatus.InProgress, TaskItemStatus.Cancelled],
        [TaskItemStatus.Completed]  = [],
        [TaskItemStatus.Cancelled]  = [],
    };

    public static bool CanTransition(ContentIdeaStatus from, ContentIdeaStatus to) =>
        _ideaTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool CanTransition(ContentDraftStatus from, ContentDraftStatus to) =>
        _draftTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool CanTransition(PublishJobStatus from, PublishJobStatus to) =>
        _publishTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool CanTransition(TaskItemStatus from, TaskItemStatus to) =>
        _taskTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static ContentIdeaStatus[] AllowedTransitions(ContentIdeaStatus from) =>
        _ideaTransitions.GetValueOrDefault(from, []);

    public static ContentDraftStatus[] AllowedTransitions(ContentDraftStatus from) =>
        _draftTransitions.GetValueOrDefault(from, []);

    public static PublishJobStatus[] AllowedTransitions(PublishJobStatus from) =>
        _publishTransitions.GetValueOrDefault(from, []);

    public static TaskItemStatus[] AllowedTransitions(TaskItemStatus from) =>
        _taskTransitions.GetValueOrDefault(from, []);
}
