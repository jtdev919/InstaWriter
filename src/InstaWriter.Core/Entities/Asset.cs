using System.Text.Json.Serialization;

namespace InstaWriter.Core.Entities;

public class Asset
{
    public Guid Id { get; set; }
    public AssetType AssetType { get; set; } = AssetType.Image;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? BlobUri { get; set; }
    public string? ThumbnailUri { get; set; }
    public string? Owner { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Uploaded;
    public string? Tags { get; set; }
    public string? PillarName { get; set; }
    public Guid? ContentIdeaId { get; set; }
    public Guid? ContentDraftId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ContentIdea? ContentIdea { get; set; }
    public ContentDraft? ContentDraft { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssetType
{
    Image,
    Video,
    Screenshot,
    Mockup,
    QuoteCard,
    Carousel,
    Logo
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssetStatus
{
    Uploaded,
    Processing,
    Ready,
    Archived
}
