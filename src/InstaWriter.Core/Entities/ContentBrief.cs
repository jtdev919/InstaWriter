using System.Text.Json.Serialization;

namespace InstaWriter.Core.Entities;

public class ContentBrief
{
    public Guid Id { get; set; }
    public Guid ContentIdeaId { get; set; }
    public ContentFormat TargetFormat { get; set; } = ContentFormat.StaticImage;
    public string Objective { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string HookDirection { get; set; } = string.Empty;
    public string KeyMessage { get; set; } = string.Empty;
    public string CTA { get; set; } = string.Empty;
    public bool RequiresOriginalMedia { get; set; }
    public bool RequiresManualApproval { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ContentIdea? ContentIdea { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentFormat
{
    StaticImage,
    Carousel,
    Reel,
    Video,
    Story
}
