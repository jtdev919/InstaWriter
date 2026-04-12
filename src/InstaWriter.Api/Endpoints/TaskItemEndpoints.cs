using InstaWriter.Core.Entities;
using InstaWriter.Core.Workflow;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class TaskItemEndpoints
{
    public static RouteGroupBuilder MapTaskItemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var tasks = await db.TaskItems
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return Results.Ok(tasks);
        }).WithName("GetTaskItems");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var task = await db.TaskItems.FindAsync(id);
            return task is not null ? Results.Ok(task) : Results.NotFound();
        }).WithName("GetTaskItemById");

        group.MapPost("/", async (TaskItem task, AppDbContext db) =>
        {
            task.Id = Guid.NewGuid();
            task.CreatedAt = DateTime.UtcNow;
            task.Status = TaskItemStatus.Pending;

            db.TaskItems.Add(task);
            await db.SaveChangesAsync();

            return Results.Created($"/api/tasks/{task.Id}", task);
        }).WithName("CreateTaskItem");

        group.MapPut("/{id:guid}", async (Guid id, TaskItem updated, AppDbContext db) =>
        {
            var task = await db.TaskItems.FindAsync(id);
            if (task is null) return Results.NotFound();

            task.Owner = updated.Owner;
            task.DueDate = updated.DueDate;
            task.TaskType = updated.TaskType;
            task.Priority = updated.Priority;
            task.Description = updated.Description;

            await db.SaveChangesAsync();
            return Results.Ok(task);
        }).WithName("UpdateTaskItem");

        group.MapPost("/{id:guid}/transition", async (Guid id, TransitionRequest request, AppDbContext db) =>
        {
            var task = await db.TaskItems.FindAsync(id);
            if (task is null) return Results.NotFound();

            if (!Enum.TryParse<TaskItemStatus>(request.Status, true, out var newStatus))
                return Results.BadRequest(new { Error = $"Invalid status '{request.Status}'.", Allowed = StatusTransitions.AllowedTransitions(task.Status) });

            if (!StatusTransitions.CanTransition(task.Status, newStatus))
                return Results.BadRequest(new { Error = $"Cannot transition from '{task.Status}' to '{newStatus}'.", Allowed = StatusTransitions.AllowedTransitions(task.Status) });

            task.Status = newStatus;
            await db.SaveChangesAsync();

            return Results.Ok(task);
        }).WithName("TransitionTaskItem");

        group.MapPost("/{id:guid}/complete", async (Guid id, AppDbContext db) =>
        {
            var task = await db.TaskItems.FindAsync(id);
            if (task is null) return Results.NotFound();

            if (!StatusTransitions.CanTransition(task.Status, TaskItemStatus.Completed))
                return Results.BadRequest(new { Error = $"Cannot complete a task in '{task.Status}' status.", Allowed = StatusTransitions.AllowedTransitions(task.Status) });

            task.Status = TaskItemStatus.Completed;
            await db.SaveChangesAsync();

            return Results.Ok(task);
        }).WithName("CompleteTaskItem");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var task = await db.TaskItems.FindAsync(id);
            if (task is null) return Results.NotFound();

            db.TaskItems.Remove(task);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteTaskItem");

        return group;
    }
}
