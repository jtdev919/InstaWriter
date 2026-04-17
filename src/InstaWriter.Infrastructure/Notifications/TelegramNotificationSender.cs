using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Notifications;

public class TelegramNotificationSender
{
    private readonly HttpClient _http;
    private readonly string _botToken;
    private readonly string _chatId;
    private readonly ILogger<TelegramNotificationSender> _logger;

    public TelegramNotificationSender(HttpClient http, IConfiguration config, ILogger<TelegramNotificationSender> logger)
    {
        _http = http;
        _botToken = config["Telegram:BotToken"] ?? "";
        _chatId = config["Telegram:ChatId"] ?? "";
        _logger = logger;
    }

    public async Task SendAsync(string message)
    {
        if (string.IsNullOrEmpty(_botToken) || string.IsNullOrEmpty(_chatId))
        {
            _logger.LogWarning("Telegram not configured — skipping notification");
            return;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var payload = new { chat_id = _chatId, text = message };
            var response = await _http.PostAsJsonAsync(url, payload);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Telegram notification sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram notification");
        }
    }
}
