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
                        "💪 Daily Engagement Time\n" +
                        "Estimated time: 30 minutes\n\n" +
                        "Your 10-10-10 routine:\n" +
                        "• 10 min -- reply to all comments on your posts\n" +
                        "• 10 min -- comment on 10 accounts in your niche\n" +
                        "• 10 min -- engage with 10 Stories via DMs\n\n" +
                        "30 minutes. Non-negotiable. Go!");
                }

                // Monday 8 AM — content creation day
                if (day == DayOfWeek.Monday && hour == 8)
                {
                    await telegram.SendAsync(
                        "📋 Monday -- Content Creation Day\n" +
                        "Estimated time: 45-60 minutes\n" +
                        "Publish target: Wednesday 8:00 AM & Friday 8:00 AM\n\n" +
                        "Create this week's carousels:\n" +
                        "1. Open InstaWriter dashboard (5 min)\n" +
                        "2. Create 3-4 content ideas (10 min)\n" +
                        "3. Generate AI drafts (5 min)\n" +
                        "4. Edit slides in carousel editor (20-30 min)\n" +
                        "5. Approve and schedule (5 min)\n\n" +
                        "Dashboard: https://instawriterstorage.z13.web.core.windows.net/");
                }

                // Tuesday 8 AM — filming day
                if (day == DayOfWeek.Tuesday && hour == 8)
                {
                    await telegram.SendAsync(
                        "🎬 Tuesday -- Reel Filming Day\n" +
                        "Estimated time: 30-45 minutes\n" +
                        "Publish target: Tuesday 12:00 PM, Thursday 12:00 PM, Saturday 10:00 AM\n\n" +
                        "Batch film 2-3 Reels:\n" +
                        "• Talking head tip, 30-60 sec (10 min)\n" +
                        "• App demo or behind-the-scenes (10 min)\n" +
                        "• Day in the life / workout clip (10 min)\n\n" +
                        "Change shirts between takes for variety.");
                }

                // Wednesday 8 AM — schedule and post
                if (day == DayOfWeek.Wednesday && hour == 8)
                {
                    await telegram.SendAsync(
                        "📅 Wednesday -- Schedule and Post\n" +
                        "Estimated time: 15-20 minutes\n" +
                        "Publish target: TODAY 12:00 PM (carousel)\n\n" +
                        "1. Review and approve pending drafts (5 min)\n" +
                        "2. Create publish jobs with schedule times (5 min)\n" +
                        "3. Post today's carousel (5 min)\n\n" +
                        "Dashboard: https://instawriterstorage.z13.web.core.windows.net/");
                }

                // Thursday 12 PM — reel post reminder
                if (day == DayOfWeek.Thursday && hour == 12)
                {
                    await telegram.SendAsync(
                        "🎬 Thursday -- Post a Reel\n" +
                        "Estimated time: 5-10 minutes\n" +
                        "Publish target: NOW (12:00 PM)\n\n" +
                        "Post one of the Reels you filmed Tuesday.\n" +
                        "Add caption + hashtags, then post.\n" +
                        "Spend 15 min engaging on the platform right after posting.");
                }

                // Friday 8 AM — carousel + engagement post
                if (day == DayOfWeek.Friday && hour == 8)
                {
                    await telegram.SendAsync(
                        "🔥 Friday -- Carousel + Engagement Post\n" +
                        "Estimated time: 15-20 minutes\n" +
                        "Publish target: TODAY 12:00 PM (carousel), 5:00 PM (engagement post)\n\n" +
                        "Morning: Post this week's second carousel (5 min)\n" +
                        "Afternoon: Post an engagement-bait post (10 min):\n" +
                        "• Hot take or controversial opinion\n" +
                        "• Poll or question\n" +
                        "• \"What's your experience with [topic]?\"");
                }

                // Saturday 10 AM — reel + stories
                if (day == DayOfWeek.Saturday && hour == 10)
                {
                    await telegram.SendAsync(
                        "📱 Saturday -- Reel + Stories Day\n" +
                        "Estimated time: 15-20 minutes\n" +
                        "Publish target: NOW 10:00 AM (reel)\n\n" +
                        "1. Post your 3rd Reel of the week (5 min)\n" +
                        "2. Post 3-5 casual Stories throughout the day:\n" +
                        "   • Behind the scenes\n" +
                        "   • Weekend routine\n" +
                        "   • Something personal\n\n" +
                        "Weekends are high-engagement on Instagram.");
                }

                // Sunday 10 AM — weekly review
                if (day == DayOfWeek.Sunday && hour == 10)
                {
                    await telegram.SendAsync(
                        "📊 Sunday -- Weekly Analytics Review\n" +
                        "Estimated time: 20-30 minutes\n" +
                        "No publish today -- rest and plan\n\n" +
                        "Check Instagram Insights:\n" +
                        "• Which posts got the most saves and shares? (5 min)\n" +
                        "• Follower growth this week? (2 min)\n" +
                        "• Best posting times for YOUR audience? (3 min)\n" +
                        "• Plan next week's content topics (10-15 min)\n\n" +
                        "Feed winners back into Monday's content ideas.\n\n" +
                        "Dashboard: https://instawriterstorage.z13.web.core.windows.net/");
                }

                // Daily 6 PM EST — evening story reminder
                if (hour == 18)
                {
                    await telegram.SendAsync(
                        "📱 Evening Stories\n" +
                        "Estimated time: 5-10 minutes\n\n" +
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
