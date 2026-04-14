using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        group.MapGet("/", async (AppDbContext db, string? recipient, bool? unreadOnly) =>
        {
            var query = db.Notifications.AsQueryable();

            if (!string.IsNullOrEmpty(recipient))
                query = query.Where(n => n.Recipient == recipient);

            if (unreadOnly == true)
                query = query.Where(n => !n.IsRead);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();

            return Results.Ok(notifications);
        }).WithName("GetNotifications");

        group.MapGet("/unread-count", async (AppDbContext db, string? recipient) =>
        {
            var query = db.Notifications.Where(n => !n.IsRead);

            if (!string.IsNullOrEmpty(recipient))
                query = query.Where(n => n.Recipient == recipient);

            var count = await query.CountAsync();
            return Results.Ok(new { count });
        }).WithName("GetUnreadCount");

        group.MapPost("/{id:guid}/read", async (Guid id, AppDbContext db) =>
        {
            var notification = await db.Notifications.FindAsync(id);
            if (notification is null) return Results.NotFound();

            notification.IsRead = true;
            await db.SaveChangesAsync();
            return Results.Ok(notification);
        }).WithName("MarkNotificationRead");

        group.MapPost("/read-all", async (AppDbContext db, string? recipient) =>
        {
            var query = db.Notifications.Where(n => !n.IsRead);

            if (!string.IsNullOrEmpty(recipient))
                query = query.Where(n => n.Recipient == recipient);

            var unread = await query.ToListAsync();
            foreach (var n in unread) n.IsRead = true;
            await db.SaveChangesAsync();

            return Results.Ok(new { marked = unread.Count });
        }).WithName("MarkAllRead");

        return group;
    }
}
