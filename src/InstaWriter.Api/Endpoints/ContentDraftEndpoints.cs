using InstaWriter.Core.Entities;
using InstaWriter.Core.Workflow;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class ContentDraftEndpoints
{
    public static RouteGroupBuilder MapContentDraftEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/content/drafts").WithTags("Content Drafts");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var drafts = await db.ContentDrafts
                .Include(d => d.ContentIdea)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            return Results.Ok(drafts);
        }).WithName("GetContentDrafts");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var draft = await db.ContentDrafts
                .Include(d => d.ContentIdea)
                .FirstOrDefaultAsync(d => d.Id == id);
            return draft is not null ? Results.Ok(draft) : Results.NotFound();
        }).WithName("GetContentDraftById");

        group.MapPost("/", async (ContentDraft draft, AppDbContext db) =>
        {
            var ideaExists = await db.ContentIdeas.AnyAsync(i => i.Id == draft.ContentIdeaId);
            if (!ideaExists)
                return Results.BadRequest(new { Error = "ContentIdeaId does not reference a valid content idea." });

            draft.Id = Guid.NewGuid();
            draft.CreatedAt = DateTime.UtcNow;
            draft.Status = ContentDraftStatus.Draft;
            draft.VersionNo = 1;

            db.ContentDrafts.Add(draft);
            await db.SaveChangesAsync();

            return Results.Created($"/api/content/drafts/{draft.Id}", draft);
        }).WithName("CreateContentDraft");

        group.MapPut("/{id:guid}", async (Guid id, ContentDraft updated, AppDbContext db) =>
        {
            var draft = await db.ContentDrafts.FindAsync(id);
            if (draft is null) return Results.NotFound();

            draft.Caption = updated.Caption;
            draft.Script = updated.Script;
            draft.CarouselCopyJson = updated.CarouselCopyJson;
            draft.HashtagSet = updated.HashtagSet;
            draft.CoverText = updated.CoverText;
            draft.ComplianceScore = updated.ComplianceScore;

            await db.SaveChangesAsync();
            return Results.Ok(draft);
        }).WithName("UpdateContentDraft");

        group.MapPost("/{id:guid}/transition", async (Guid id, TransitionRequest request, AppDbContext db) =>
        {
            var draft = await db.ContentDrafts.FindAsync(id);
            if (draft is null) return Results.NotFound();

            if (!Enum.TryParse<ContentDraftStatus>(request.Status, true, out var newStatus))
                return Results.BadRequest(new { Error = $"Invalid status '{request.Status}'.", Allowed = StatusTransitions.AllowedTransitions(draft.Status) });

            if (!StatusTransitions.CanTransition(draft.Status, newStatus))
                return Results.BadRequest(new { Error = $"Cannot transition from '{draft.Status}' to '{newStatus}'.", Allowed = StatusTransitions.AllowedTransitions(draft.Status) });

            draft.Status = newStatus;
            await db.SaveChangesAsync();

            return Results.Ok(draft);
        }).WithName("TransitionContentDraft");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var draft = await db.ContentDrafts.FindAsync(id);
            if (draft is null) return Results.NotFound();

            db.ContentDrafts.Remove(draft);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteContentDraft");

        return group;
    }
}
