using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Notifications;

public class NotificationService(
    AppDbContext db,
    IEnumerable<INotificationChannelSender> channelSenders,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        // Always persist as in-app notification
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Recipient = request.Recipient,
            Channel = request.Channel,
            Subject = request.Subject,
            Body = request.Body,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        // Dispatch to the appropriate channel sender
        if (request.Channel != NotificationChannel.InApp)
        {
            var sender = channelSenders.FirstOrDefault(s => s.Channel == request.Channel);
            if (sender != null)
            {
                try
                {
                    await sender.SendAsync(request.Recipient, request.Subject, request.Body, ct);
                    logger.LogInformation("Sent {Channel} notification to {Recipient}: {Subject}",
                        request.Channel, request.Recipient, request.Subject);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send {Channel} notification to {Recipient}",
                        request.Channel, request.Recipient);
                }
            }
            else
            {
                logger.LogWarning("No sender registered for channel {Channel}", request.Channel);
            }
        }
    }

    public async Task SendToAllChannelsAsync(string recipient, string subject, string body,
        string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken ct = default)
    {
        // Always send in-app
        await SendAsync(new NotificationRequest(recipient, subject, body, NotificationChannel.InApp, relatedEntityType, relatedEntityId), ct);

        // Send to all configured external channels
        foreach (var sender in channelSenders)
        {
            try
            {
                await sender.SendAsync(recipient, subject, body, ct);
                logger.LogInformation("Sent {Channel} notification to {Recipient}", sender.Channel, recipient);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send {Channel} notification to {Recipient}", sender.Channel, recipient);
            }
        }
    }
}
