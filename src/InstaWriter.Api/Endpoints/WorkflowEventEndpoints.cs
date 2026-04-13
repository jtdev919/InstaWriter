using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class WorkflowEventEndpoints
{
    public static RouteGroupBuilder MapWorkflowEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/workflow-events").WithTags("WorkflowEvents");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var events = await db.WorkflowEvents.OrderByDescending(e => e.EventTime).ToListAsync();
            return Results.Ok(events);
        }).WithName("GetWorkflowEvents");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var wfEvent = await db.WorkflowEvents.FindAsync(id);
            return wfEvent is not null ? Results.Ok(wfEvent) : Results.NotFound();
        }).WithName("GetWorkflowEventById");

        group.MapGet("/by-entity/{entityType}/{entityId:guid}", async (string entityType, Guid entityId, AppDbContext db) =>
        {
            var events = await db.WorkflowEvents
                .Where(e => e.EntityType == entityType && e.EntityId == entityId)
                .OrderByDescending(e => e.EventTime)
                .ToListAsync();
            return Results.Ok(events);
        }).WithName("GetWorkflowEventsByEntity");

        group.MapGet("/by-correlation/{correlationId}", async (string correlationId, AppDbContext db) =>
        {
            var events = await db.WorkflowEvents
                .Where(e => e.CorrelationId == correlationId)
                .OrderBy(e => e.EventTime)
                .ToListAsync();
            return Results.Ok(events);
        }).WithName("GetWorkflowEventsByCorrelation");

        group.MapPost("/", async (WorkflowEvent wfEvent, AppDbContext db) =>
        {
            wfEvent.Id = Guid.NewGuid();
            wfEvent.EventTime = DateTime.UtcNow;

            db.WorkflowEvents.Add(wfEvent);
            await db.SaveChangesAsync();

            return Results.Created($"/api/workflow-events/{wfEvent.Id}", wfEvent);
        }).WithName("CreateWorkflowEvent");

        return group;
    }
}
