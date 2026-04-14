using InstaWriter.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Notifications;

/// <summary>
/// Slack notification sender via incoming webhook.
/// Configure Slack:WebhookUrl in appsettings. When not configured, logs instead.
/// </summary>
public class SlackNotificationSender(IConfiguration config, ILogger<SlackNotificationSender> logger) : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.Slack;

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
    {
        var webhookUrl = config["Slack:WebhookUrl"];

        if (string.IsNullOrEmpty(webhookUrl))
        {
            logger.LogInformation("[Slack] (not configured) To: {Recipient}, Subject: {Subject}", recipient, subject);
            return;
        }

        using var httpClient = new HttpClient();
        var payload = new { text = $"*{subject}*\n{body}\n_Recipient: {recipient}_" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(webhookUrl, content, ct);

        if (response.IsSuccessStatusCode)
            logger.LogInformation("Slack notification sent: {Subject}", subject);
        else
            logger.LogError("Slack webhook failed with {StatusCode}", response.StatusCode);
    }
}
