namespace InstaWriter.Core.Services;

public interface INotificationService
{
    Task SendAsync(NotificationRequest request, CancellationToken ct = default);
    Task SendToAllChannelsAsync(string recipient, string subject, string body, string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken ct = default);
}

public record NotificationRequest(
    string Recipient,
    string Subject,
    string Body,
    Core.Entities.NotificationChannel Channel = Core.Entities.NotificationChannel.InApp,
    string? RelatedEntityType = null,
    Guid? RelatedEntityId = null);
