using InstaWriter.Core.Entities;
using InstaWriter.Core.Workflow;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class PublishJobEndpoints
{
    public static RouteGroupBuilder MapPublishJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/publish/jobs").WithTags("Publish Jobs");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var jobs = await db.PublishJobs
                .Include(j => j.ContentDraft)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
            return Results.Ok(jobs);
        }).WithName("GetPublishJobs");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var job = await db.PublishJobs
                .Include(j => j.ContentDraft)
                .FirstOrDefaultAsync(j => j.Id == id);
            return job is not null ? Results.Ok(job) : Results.NotFound();
        }).WithName("GetPublishJobById");

        group.MapPost("/", async (PublishJob job, AppDbContext db) =>
        {
            var draftExists = await db.ContentDrafts.AnyAsync(d => d.Id == job.ContentDraftId);
            if (!draftExists)
                return Results.BadRequest(new { Error = "ContentDraftId does not reference a valid content draft." });

            job.Id = Guid.NewGuid();
            job.CreatedAt = DateTime.UtcNow;
            job.Status = PublishJobStatus.Pending;

            db.PublishJobs.Add(job);
            await db.SaveChangesAsync();

            return Results.Created($"/api/publish/jobs/{job.Id}", job);
        }).WithName("CreatePublishJob");

        group.MapPost("/{id:guid}/transition", async (Guid id, TransitionRequest request, AppDbContext db) =>
        {
            var job = await db.PublishJobs.FindAsync(id);
            if (job is null) return Results.NotFound();

            if (!Enum.TryParse<PublishJobStatus>(request.Status, true, out var newStatus))
                return Results.BadRequest(new { Error = $"Invalid status '{request.Status}'.", Allowed = StatusTransitions.AllowedTransitions(job.Status) });

            if (!StatusTransitions.CanTransition(job.Status, newStatus))
                return Results.BadRequest(new { Error = $"Cannot transition from '{job.Status}' to '{newStatus}'.", Allowed = StatusTransitions.AllowedTransitions(job.Status) });

            job.Status = newStatus;
            await db.SaveChangesAsync();

            return Results.Ok(job);
        }).WithName("TransitionPublishJob");

        group.MapGet("/{id:guid}/status", async (Guid id, AppDbContext db) =>
        {
            var job = await db.PublishJobs.FindAsync(id);
            if (job is null) return Results.NotFound();

            return Results.Ok(new
            {
                job.Id,
                job.Status,
                job.PlannedPublishDate,
                job.ExternalMediaId,
                job.FailureReason
            });
        }).WithName("GetPublishJobStatus");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var job = await db.PublishJobs.FindAsync(id);
            if (job is null) return Results.NotFound();

            db.PublishJobs.Remove(job);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeletePublishJob");

        return group;
    }
}
