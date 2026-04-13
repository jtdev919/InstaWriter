using System.Text.Json.Serialization;

namespace InstaWriter.Core.Entities;

public class Campaign
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public string? AudienceSegment { get; set; }
    public string? KPISet { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CampaignStatus
{
    Draft,
    Active,
    Paused,
    Completed,
    Archived
}
