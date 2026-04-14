using System.Collections.Concurrent;
using InstaWriter.Core.Services;

namespace InstaWriter.Api.Tests.Fakes;

public class FakeNotificationService : INotificationService
{
    public ConcurrentBag<NotificationRequest> SentNotifications { get; } = [];

    public Task SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        SentNotifications.Add(request);
        return Task.CompletedTask;
    }

    public Task SendToAllChannelsAsync(string recipient, string subject, string body,
        string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken ct = default)
    {
        SentNotifications.Add(new NotificationRequest(recipient, subject, body,
            Core.Entities.NotificationChannel.InApp, relatedEntityType, relatedEntityId));
        return Task.CompletedTask;
    }
}
