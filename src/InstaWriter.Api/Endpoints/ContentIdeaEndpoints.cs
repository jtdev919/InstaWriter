using InstaWriter.Core.Entities;
using InstaWriter.Core.Workflow;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class ContentIdeaEndpoints
{
    public static RouteGroupBuilder MapContentIdeaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/content/ideas").WithTags("Content Ideas");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var ideas = await db.ContentIdeas.OrderByDescending(x => x.CreatedAt).ToListAsync();
            return Results.Ok(ideas);
        }).WithName("GetContentIdeas");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var idea = await db.ContentIdeas.FindAsync(id);
            return idea is not null ? Results.Ok(idea) : Results.NotFound();
        }).WithName("GetContentIdeaById");

        group.MapPost("/", async (ContentIdea idea, AppDbContext db) =>
        {
            idea.Id = Guid.NewGuid();
            idea.CreatedAt = DateTime.UtcNow;
            idea.Status = ContentIdeaStatus.Captured;

            db.ContentIdeas.Add(idea);
            await db.SaveChangesAsync();

            return Results.Created($"/api/content/ideas/{idea.Id}", idea);
        }).WithName("CreateContentIdea");

        group.MapPut("/{id:guid}", async (Guid id, ContentIdea updated, AppDbContext db) =>
        {
            var idea = await db.ContentIdeas.FindAsync(id);
            if (idea is null) return Results.NotFound();

            idea.Title = updated.Title;
            idea.Summary = updated.Summary;
            idea.SourceType = updated.SourceType;
            idea.PillarName = updated.PillarName;
            idea.RiskLevel = updated.RiskLevel;
            idea.PlannedPublishDate = updated.PlannedPublishDate;

            await db.SaveChangesAsync();
            return Results.Ok(idea);
        }).WithName("UpdateContentIdea");

        group.MapPost("/{id:guid}/transition", async (Guid id, TransitionRequest request, AppDbContext db) =>
        {
            var idea = await db.ContentIdeas.FindAsync(id);
            if (idea is null) return Results.NotFound();

            if (!Enum.TryParse<ContentIdeaStatus>(request.Status, true, out var newStatus))
                return Results.BadRequest(new { Error = $"Invalid status '{request.Status}'.", Allowed = StatusTransitions.AllowedTransitions(idea.Status) });

            if (!StatusTransitions.CanTransition(idea.Status, newStatus))
                return Results.BadRequest(new { Error = $"Cannot transition from '{idea.Status}' to '{newStatus}'.", Allowed = StatusTransitions.AllowedTransitions(idea.Status) });

            idea.Status = newStatus;
            await db.SaveChangesAsync();

            return Results.Ok(idea);
        }).WithName("TransitionContentIdea");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var idea = await db.ContentIdeas.FindAsync(id);
            if (idea is null) return Results.NotFound();

            db.ContentIdeas.Remove(idea);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteContentIdea");

        return group;
    }
}
