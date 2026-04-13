using System.Text.Json.Serialization;

namespace InstaWriter.Core.Entities;

public class ContentIdea
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? SourceType { get; set; }
    public string? PillarName { get; set; }
    public ContentRiskLevel RiskLevel { get; set; } = ContentRiskLevel.Low;
    public ContentIdeaStatus Status { get; set; } = ContentIdeaStatus.Captured;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PlannedPublishDate { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentRiskLevel
{
    Low,
    Medium,
    High
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentIdeaStatus
{
    Captured,
    Planned,
    InProgress,
    Published,
    Archived,
    Rejected
}
