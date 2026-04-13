using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class ContentBriefEndpoints
{
    public static RouteGroupBuilder MapContentBriefEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/content/briefs").WithTags("ContentBriefs");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var briefs = await db.ContentBriefs
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return Results.Ok(briefs);
        }).WithName("GetContentBriefs");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var brief = await db.ContentBriefs.FindAsync(id);
            return brief is not null ? Results.Ok(brief) : Results.NotFound();
        }).WithName("GetContentBriefById");

        group.MapGet("/by-idea/{ideaId:guid}", async (Guid ideaId, AppDbContext db) =>
        {
            var briefs = await db.ContentBriefs
                .Where(b => b.ContentIdeaId == ideaId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return Results.Ok(briefs);
        }).WithName("GetContentBriefsByIdea");

        group.MapPost("/", async (ContentBrief brief, AppDbContext db) =>
        {
            var ideaExists = await db.ContentIdeas.AnyAsync(i => i.Id == brief.ContentIdeaId);
            if (!ideaExists) return Results.BadRequest("ContentIdea not found.");

            brief.Id = Guid.NewGuid();
            brief.CreatedAt = DateTime.UtcNow;

            db.ContentBriefs.Add(brief);
            await db.SaveChangesAsync();

            return Results.Created($"/api/content/briefs/{brief.Id}", brief);
        }).WithName("CreateContentBrief");

        group.MapPut("/{id:guid}", async (Guid id, ContentBrief updated, AppDbContext db) =>
        {
            var brief = await db.ContentBriefs.FindAsync(id);
            if (brief is null) return Results.NotFound();

            brief.TargetFormat = updated.TargetFormat;
            brief.Objective = updated.Objective;
            brief.Audience = updated.Audience;
            brief.HookDirection = updated.HookDirection;
            brief.KeyMessage = updated.KeyMessage;
            brief.CTA = updated.CTA;
            brief.RequiresOriginalMedia = updated.RequiresOriginalMedia;
            brief.RequiresManualApproval = updated.RequiresManualApproval;

            await db.SaveChangesAsync();
            return Results.Ok(brief);
        }).WithName("UpdateContentBrief");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var brief = await db.ContentBriefs.FindAsync(id);
            if (brief is null) return Results.NotFound();

            db.ContentBriefs.Remove(brief);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteContentBrief");

        return group;
    }
}
