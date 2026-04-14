using System.Text.Json.Serialization;

namespace InstaWriter.Core.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationChannel
{
    InApp,
    Email,
    Slack,
    Sms,
    Push
}
