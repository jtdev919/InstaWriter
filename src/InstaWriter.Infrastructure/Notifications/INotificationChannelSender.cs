using InstaWriter.Core.Entities;

namespace InstaWriter.Infrastructure.Notifications;

public interface INotificationChannelSender
{
    NotificationChannel Channel { get; }
    Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default);
}
