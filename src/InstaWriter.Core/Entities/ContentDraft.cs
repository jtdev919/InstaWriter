using System.Text.Json.Serialization;

namespace InstaWriter.Core.Entities;

public class ContentDraft
{
    public Guid Id { get; set; }
    public Guid ContentIdeaId { get; set; }
    public string Caption { get; set; } = string.Empty;
    public string? Script { get; set; }
    public string? CarouselCopyJson { get; set; }
    public string? HashtagSet { get; set; }
    public string? CoverText { get; set; }
    public double? ComplianceScore { get; set; }
    public int VersionNo { get; set; } = 1;
    public ContentDraftStatus Status { get; set; } = ContentDraftStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ContentIdea? ContentIdea { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentDraftStatus
{
    Draft,
    AwaitingReview,
    Approved,
    Rejected,
    Published
}
