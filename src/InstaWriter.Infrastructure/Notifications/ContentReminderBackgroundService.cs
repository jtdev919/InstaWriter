using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow.AddHours(-4); // EST
                var hour = now.Hour;
                var day = now.DayOfWeek;
                var today = now.Date;

                using var scope = _services.CreateScope();
                var telegram = scope.ServiceProvider.GetRequiredService<TelegramNotificationSender>();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Check if we already created tasks for today (avoid duplicates on restart)
                var alreadyCreated = await db.TaskItems
                    .AnyAsync(t => t.CreatedAt.Date == DateTime.UtcNow.Date
                        && t.TaskType == "ContentReminder"
                        && t.Owner == "system", stoppingToken);

                if (!alreadyCreated && hour == 7)
                {
                    // Create today's tasks and calendar events
                    await CreateDailyTasks(db, telegram, day, today, stoppingToken);
                }

                // Send time-specific reminders (no task creation, just nudges)
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

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CreateDailyTasks(AppDbContext db, TelegramNotificationSender telegram, DayOfWeek day, DateTime today, CancellationToken ct)
    {
        var tasks = new List<(string title, string description, string message, int durationMin, DateTime start, DateTime end, TaskPriority priority)>();

        // Daily engagement task
        tasks.Add((
            "10-10-10 Engagement",
            "Reply to all comments, comment on 10 niche accounts, engage with 10 Stories via DMs",
            "",
            30,
            today.AddHours(9),
            today.AddHours(9).AddMinutes(30),
            TaskPriority.High
        ));

        // Daily stories
        tasks.Add((
            "Evening Stories",
            "Post 2-3 Stories: behind the scenes, health tip, poll or question",
            "",
            10,
            today.AddHours(18),
            today.AddHours(18).AddMinutes(10),
            TaskPriority.Medium
        ));

        // Day-specific tasks
        switch (day)
        {
            case DayOfWeek.Monday:
                tasks.Add((
                    "Create Carousel Drafts",
                    "Open InstaWriter dashboard. Create 3-4 content ideas, generate AI drafts, edit slides in carousel editor, approve and schedule.",
                    "📋 Monday -- Content Creation Day\nEstimated time: 45-60 minutes\nPublish target: Wednesday 12:00 PM & Friday 12:00 PM\n\nDashboard: https://instawriterstorage.z13.web.core.windows.net/",
                    60,
                    today.AddHours(8),
                    today.AddHours(9),
                    TaskPriority.High
                ));
                break;

            case DayOfWeek.Tuesday:
                tasks.Add((
                    "Film 2-3 Reels",
                    "Batch film: talking head tip (30-60 sec), app demo or behind-the-scenes, day in the life or workout clip. Change shirts between takes.",
                    "🎬 Tuesday -- Reel Filming Day\nEstimated time: 30-45 minutes\nPublish target: Tuesday 12:00 PM, Thursday 12:00 PM, Saturday 10:00 AM",
                    45,
                    today.AddHours(8),
                    today.AddHours(8).AddMinutes(45),
                    TaskPriority.High
                ));
                tasks.Add((
                    "Post Reel",
                    "Post one of the Reels you just filmed. Add caption + hashtags. Spend 15 min engaging after posting.",
                    "",
                    15,
                    today.AddHours(12),
                    today.AddHours(12).AddMinutes(15),
                    TaskPriority.High
                ));
                break;

            case DayOfWeek.Wednesday:
                tasks.Add((
                    "Schedule and Post Carousel",
                    "Review and approve pending drafts. Create publish jobs with schedule times. Post today's carousel.",
                    "📅 Wednesday -- Schedule and Post\nEstimated time: 15-20 minutes\nPublish target: TODAY 12:00 PM\n\nDashboard: https://instawriterstorage.z13.web.core.windows.net/",
                    20,
                    today.AddHours(8),
                    today.AddHours(8).AddMinutes(20),
                    TaskPriority.High
                ));
                break;

            case DayOfWeek.Thursday:
                tasks.Add((
                    "Post Reel",
                    "Post one of the Reels from Tuesday's filming session. Add caption + hashtags. Spend 15 min engaging after posting.",
                    "🎬 Thursday -- Post a Reel\nEstimated time: 5-10 minutes\nPublish target: NOW (12:00 PM)",
                    10,
                    today.AddHours(12),
                    today.AddHours(12).AddMinutes(10),
                    TaskPriority.High
                ));
                break;

            case DayOfWeek.Friday:
                tasks.Add((
                    "Post Carousel",
                    "Post this week's second carousel from approved drafts.",
                    "",
                    10,
                    today.AddHours(12),
                    today.AddHours(12).AddMinutes(10),
                    TaskPriority.High
                ));
                tasks.Add((
                    "Engagement Post",
                    "Post an engagement-bait post: hot take, poll, question, or controversial opinion. Get people commenting.",
                    "🔥 Friday -- Carousel + Engagement Post\nEstimated time: 15-20 minutes\nPublish target: 12:00 PM (carousel), 5:00 PM (engagement)",
                    15,
                    today.AddHours(17),
                    today.AddHours(17).AddMinutes(15),
                    TaskPriority.Medium
                ));
                break;

            case DayOfWeek.Saturday:
                tasks.Add((
                    "Post Reel + Stories",
                    "Post your 3rd Reel of the week. Post 3-5 casual Stories throughout the day: behind the scenes, weekend routine, something personal.",
                    "📱 Saturday -- Reel + Stories Day\nEstimated time: 15-20 minutes\nPublish target: NOW 10:00 AM (reel)",
                    20,
                    today.AddHours(10),
                    today.AddHours(10).AddMinutes(20),
                    TaskPriority.Medium
                ));
                break;

            case DayOfWeek.Sunday:
                tasks.Add((
                    "Weekly Analytics Review",
                    "Check Instagram Insights: which posts got saves/shares, follower growth, best posting times. Plan next week's content topics. Feed winners into Monday's content ideas.",
                    "📊 Sunday -- Weekly Analytics Review\nEstimated time: 20-30 minutes\nNo publish today -- rest and plan\n\nDashboard: https://instawriterstorage.z13.web.core.windows.net/",
                    30,
                    today.AddHours(10),
                    today.AddHours(10).AddMinutes(30),
                    TaskPriority.Medium
                ));
                break;
        }

        // Create tasks and calendar events in the database
        foreach (var (title, description, message, durationMin, start, end, priority) in tasks)
        {
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                TaskType = "ContentReminder",
                Owner = "system",
                Description = $"{title}: {description}",
                DueDate = end,
                Priority = priority,
                Status = TaskItemStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.TaskItems.Add(task);

            var calEvent = new CalendarEvent
            {
                Id = Guid.NewGuid(),
                TaskItemId = task.Id,
                StartDateTime = start,
                EndDateTime = end,
                ReminderProfile = $"{durationMin} min",
                CreatedAt = DateTime.UtcNow
            };
            db.CalendarEvents.Add(calEvent);
        }

        await db.SaveChangesAsync(ct);

        // Send the day's main Telegram message
        var mainMessages = tasks.Where(t => !string.IsNullOrEmpty(t.message)).Select(t => t.message);
        foreach (var msg in mainMessages)
        {
            await telegram.SendAsync(msg);
        }

        _logger.LogInformation("Created {Count} tasks and calendar events for {Day}", tasks.Count, day);
    }
}
