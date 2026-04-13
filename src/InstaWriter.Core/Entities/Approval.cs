using System.Text.Json.Serialization;

namespace InstaWriter.Core.Entities;

public class Approval
{
    public Guid Id { get; set; }
    public Guid ContentDraftId { get; set; }
    public string Approver { get; set; } = string.Empty;
    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;
    public string? Comments { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ContentDraft? ContentDraft { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApprovalDecision
{
    Pending,
    Approved,
    Rejected,
    RevisionRequested
}
