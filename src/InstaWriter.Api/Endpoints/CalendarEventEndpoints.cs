using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class CalendarEventEndpoints
{
    public static RouteGroupBuilder MapCalendarEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/calendar-events").WithTags("CalendarEvents");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var events = await db.CalendarEvents.OrderBy(e => e.StartDateTime).ToListAsync();
            return Results.Ok(events);
        }).WithName("GetCalendarEvents");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var calEvent = await db.CalendarEvents.FindAsync(id);
            return calEvent is not null ? Results.Ok(calEvent) : Results.NotFound();
        }).WithName("GetCalendarEventById");

        group.MapGet("/by-task/{taskId:guid}", async (Guid taskId, AppDbContext db) =>
        {
            var events = await db.CalendarEvents
                .Where(e => e.TaskItemId == taskId)
                .OrderBy(e => e.StartDateTime)
                .ToListAsync();
            return Results.Ok(events);
        }).WithName("GetCalendarEventsByTask");

        group.MapPost("/", async (CalendarEvent calEvent, AppDbContext db) =>
        {
            var taskExists = await db.TaskItems.AnyAsync(t => t.Id == calEvent.TaskItemId);
            if (!taskExists) return Results.BadRequest("TaskItem not found.");

            calEvent.Id = Guid.NewGuid();
            calEvent.CreatedAt = DateTime.UtcNow;

            db.CalendarEvents.Add(calEvent);
            await db.SaveChangesAsync();

            return Results.Created($"/api/calendar-events/{calEvent.Id}", calEvent);
        }).WithName("CreateCalendarEvent");

        group.MapPut("/{id:guid}", async (Guid id, CalendarEvent updated, AppDbContext db) =>
        {
            var calEvent = await db.CalendarEvents.FindAsync(id);
            if (calEvent is null) return Results.NotFound();

            calEvent.ExternalCalendarId = updated.ExternalCalendarId;
            calEvent.StartDateTime = updated.StartDateTime;
            calEvent.EndDateTime = updated.EndDateTime;
            calEvent.ReminderProfile = updated.ReminderProfile;

            await db.SaveChangesAsync();
            return Results.Ok(calEvent);
        }).WithName("UpdateCalendarEvent");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var calEvent = await db.CalendarEvents.FindAsync(id);
            if (calEvent is null) return Results.NotFound();

            db.CalendarEvents.Remove(calEvent);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteCalendarEvent");

        return group;
    }
}
