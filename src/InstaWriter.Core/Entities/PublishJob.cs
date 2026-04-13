using System.Text.Json.Serialization;

namespace InstaWriter.Core.Entities;

public class PublishJob
{
    public Guid Id { get; set; }
    public Guid ContentDraftId { get; set; }
    public Guid? ChannelAccountId { get; set; }
    public DateTime? PlannedPublishDate { get; set; }
    public PublishMode PublishMode { get; set; } = PublishMode.Auto;
    public string? ExternalContainerId { get; set; }
    public string? ExternalMediaId { get; set; }
    public PublishJobStatus Status { get; set; } = PublishJobStatus.Pending;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ContentDraft? ContentDraft { get; set; }
    public ChannelAccount? ChannelAccount { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PublishMode
{
    Auto,
    Manual
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PublishJobStatus
{
    Pending,
    Scheduled,
    Publishing,
    Published,
    Failed,
    Cancelled
}
