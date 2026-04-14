using InstaWriter.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Notifications;

/// <summary>
/// Email notification sender. Configure SendGrid:ApiKey and SendGrid:FromEmail in appsettings.
/// When not configured, logs the email instead of sending.
/// </summary>
public class EmailNotificationSender(IConfiguration config, ILogger<EmailNotificationSender> logger) : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
    {
        var apiKey = config["SendGrid:ApiKey"];
        var fromEmail = config["SendGrid:FromEmail"] ?? "noreply@instawriter.app";

        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogInformation("[Email] (not configured) To: {Recipient}, Subject: {Subject}, Body: {Body}",
                recipient, subject, body);
            return;
        }

        // SendGrid integration — when API key is configured, send via HTTP
        using var httpClient = new HttpClient();
        var payload = new
        {
            personalizations = new[] { new { to = new[] { new { email = recipient } } } },
            from = new { email = fromEmail },
            subject,
            content = new[] { new { type = "text/plain", value = body } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
            logger.LogInformation("Email sent to {Recipient}: {Subject}", recipient, subject);
        else
            logger.LogError("SendGrid failed with {StatusCode} for {Recipient}", response.StatusCode, recipient);
    }
}
