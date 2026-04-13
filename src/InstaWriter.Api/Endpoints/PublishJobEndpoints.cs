using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
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

        group.MapPost("/{id:guid}/execute", async (Guid id, ExecutePublishRequest request, AppDbContext db, IInstagramPublisher publisher) =>
        {
            var job = await db.PublishJobs
                .Include(j => j.ContentDraft)
                .Include(j => j.ChannelAccount)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job is null) return Results.NotFound();

            // Must be in Scheduled state to execute
            if (job.Status != PublishJobStatus.Scheduled)
                return Results.BadRequest(new { Error = $"Job must be in 'Scheduled' status to execute. Current: '{job.Status}'." });

            // Need a connected channel account
            var account = job.ChannelAccount;
            if (account is null || account.AuthStatus != AuthStatus.Connected || string.IsNullOrEmpty(account.AccessToken))
                return Results.BadRequest(new { Error = "No connected channel account with valid token." });

            if (string.IsNullOrEmpty(account.ExternalAccountId))
                return Results.BadRequest(new { Error = "Channel account missing ExternalAccountId (Instagram User ID)." });

            if (string.IsNullOrEmpty(request.ImageUrl) && string.IsNullOrEmpty(request.VideoUrl))
                return Results.BadRequest(new { Error = "Either imageUrl or videoUrl is required." });

            // Transition to Publishing
            job.Status = PublishJobStatus.Publishing;
            await db.SaveChangesAsync();

            // Execute the publish
            var caption = job.ContentDraft?.Caption ?? "";
            PublishResult result;

            if (!string.IsNullOrEmpty(request.VideoUrl))
                result = await publisher.PublishReelAsync(account.AccessToken, account.ExternalAccountId, request.VideoUrl, caption);
            else
                result = await publisher.PublishSingleImageAsync(account.AccessToken, account.ExternalAccountId, request.ImageUrl!, caption);

            if (result.Success)
            {
                job.Status = PublishJobStatus.Published;
                job.ExternalContainerId = result.ContainerId;
                job.ExternalMediaId = result.MediaId;
                job.FailureReason = null;
            }
            else
            {
                job.Status = PublishJobStatus.Failed;
                job.ExternalContainerId = result.ContainerId;
                job.FailureReason = result.Error;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                job.Id,
                job.Status,
                job.ExternalContainerId,
                job.ExternalMediaId,
                job.FailureReason
            });
        }).WithName("ExecutePublishJob");

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

    public record ExecutePublishRequest(string? ImageUrl, string? VideoUrl);
}
