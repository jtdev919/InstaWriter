using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Notifications;

public class ContentReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ContentReminderBackgroundService> _logger;

    public ContentReminderBackgroundService(IServiceProvider services, ILogger<ContentReminderBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 30 seconds for startup
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow.AddHours(-4); // EST
                var hour = now.Hour;
                var day = now.DayOfWeek;

                using var scope = _services.CreateScope();
                var telegram = scope.ServiceProvider.GetRequiredService<TelegramNotificationSender>();

                // Daily 9 AM EST — engagement reminder
                if (hour == 9)
                {
                    await telegram.SendAsync(
                        "💪 Daily Engagement Time\n\n" +
                        "Your 10-10-10 routine:\n" +
                        "• 10 min — reply to all comments on your posts\n" +
                        "• 10 min — comment on 10 accounts in your niche\n" +
                        "• 10 min — engage with 10 Stories via DMs\n\n" +
                        "30 minutes. Non-negotiable. Go! 🚀");
                }

                // Monday 8 AM — content creation day
                if (day == DayOfWeek.Monday && hour == 8)
                {
                    await telegram.SendAsync(
                        "📋 Monday -- Content Creation Day\n\n" +
                        "Time to create this week's carousels:\n" +
                        "1. Open InstaWriter dashboard\n" +
                        "2. Create 3-4 content ideas\n" +
                        "3. Generate AI drafts\n" +
                        "4. Edit slides in carousel editor\n" +
                        "5. Approve and schedule\n\n" +
                        "🔗 [Open Dashboard](https://instawriterstorage.z13.web.core.windows.net/)");
                }

                // Tuesday 8 AM — filming day
                if (day == DayOfWeek.Tuesday && hour == 8)
                {
                    await telegram.SendAsync(
                        "🎬 Tuesday -- Reel Filming Day\n\n" +
                        "Batch film 2-3 Reels today:\n" +
                        "• Talking head tip (30-60 sec)\n" +
                        "• App demo or behind-the-scenes\n" +
                        "• Day in the life / workout clip\n\n" +
                        "Change shirts between takes for variety 👔");
                }

                // Wednesday 8 AM — schedule and post
                if (day == DayOfWeek.Wednesday && hour == 8)
                {
                    await telegram.SendAsync(
                        "📅 Wednesday -- Schedule and Post\n\n" +
                        "Review and schedule the week's content:\n" +
                        "• Approve pending drafts\n" +
                        "• Create publish jobs\n" +
                        "• Post today's carousel\n\n" +
                        "🔗 [Open Dashboard](https://instawriterstorage.z13.web.core.windows.net/)");
                }

                // Friday 8 AM — engagement post
                if (day == DayOfWeek.Friday && hour == 8)
                {
                    await telegram.SendAsync(
                        "🔥 Friday -- Engagement Post Day\n\n" +
                        "Post something that drives comments:\n" +
                        "• Hot take or controversial opinion\n" +
                        "• Poll or question\n" +
                        "• \"What's your experience with [topic]?\"\n\n" +
                        "Engagement posts boost your reach for the weekend.");
                }

                // Sunday 10 AM — weekly review
                if (day == DayOfWeek.Sunday && hour == 10)
                {
                    await telegram.SendAsync(
                        "📊 Sunday -- Weekly Analytics Review\n\n" +
                        "Check your Instagram Insights:\n" +
                        "• Which posts got the most saves & shares?\n" +
                        "• What's your follower growth this week?\n" +
                        "• Best posting times for YOUR audience?\n\n" +
                        "Feed winners back into next week's content ideas.\n\n" +
                        "🔗 [Open Dashboard](https://instawriterstorage.z13.web.core.windows.net/)");
                }

                // Daily 6 PM EST — evening story reminder
                if (hour == 18)
                {
                    await telegram.SendAsync(
                        "📱 Evening Stories\n\n" +
                        "Post 2-3 Stories before bed:\n" +
                        "• Behind the scenes of your day\n" +
                        "• Quick health tip or insight\n" +
                        "• Poll or question to drive engagement");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Content reminder error");
            }

            // Check every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
