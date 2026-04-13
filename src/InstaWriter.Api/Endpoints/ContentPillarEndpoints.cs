using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class ContentPillarEndpoints
{
    public static RouteGroupBuilder MapContentPillarEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/content-pillars").WithTags("ContentPillars");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var pillars = await db.ContentPillars.OrderByDescending(p => p.PriorityWeight).ToListAsync();
            return Results.Ok(pillars);
        }).WithName("GetContentPillars");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var pillar = await db.ContentPillars.FindAsync(id);
            return pillar is not null ? Results.Ok(pillar) : Results.NotFound();
        }).WithName("GetContentPillarById");

        group.MapPost("/", async (ContentPillar pillar, AppDbContext db) =>
        {
            pillar.Id = Guid.NewGuid();
            pillar.CreatedAt = DateTime.UtcNow;

            db.ContentPillars.Add(pillar);
            await db.SaveChangesAsync();

            return Results.Created($"/api/content-pillars/{pillar.Id}", pillar);
        }).WithName("CreateContentPillar");

        group.MapPut("/{id:guid}", async (Guid id, ContentPillar updated, AppDbContext db) =>
        {
            var pillar = await db.ContentPillars.FindAsync(id);
            if (pillar is null) return Results.NotFound();

            pillar.Name = updated.Name;
            pillar.Description = updated.Description;
            pillar.PriorityWeight = updated.PriorityWeight;

            await db.SaveChangesAsync();
            return Results.Ok(pillar);
        }).WithName("UpdateContentPillar");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var pillar = await db.ContentPillars.FindAsync(id);
            if (pillar is null) return Results.NotFound();

            db.ContentPillars.Remove(pillar);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteContentPillar");

        return group;
    }
}
