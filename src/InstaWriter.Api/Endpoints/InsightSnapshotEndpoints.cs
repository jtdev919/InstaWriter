using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class InsightSnapshotEndpoints
{
    public static RouteGroupBuilder MapInsightSnapshotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/insights").WithTags("InsightSnapshots");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var snapshots = await db.InsightSnapshots.OrderByDescending(s => s.SnapshotDate).ToListAsync();
            return Results.Ok(snapshots);
        }).WithName("GetInsightSnapshots");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var snapshot = await db.InsightSnapshots.FindAsync(id);
            return snapshot is not null ? Results.Ok(snapshot) : Results.NotFound();
        }).WithName("GetInsightSnapshotById");

        group.MapGet("/by-job/{jobId:guid}", async (Guid jobId, AppDbContext db) =>
        {
            var snapshots = await db.InsightSnapshots
                .Where(s => s.PublishJobId == jobId)
                .OrderByDescending(s => s.SnapshotDate)
                .ToListAsync();
            return Results.Ok(snapshots);
        }).WithName("GetInsightSnapshotsByJob");

        group.MapPost("/", async (InsightSnapshot snapshot, AppDbContext db) =>
        {
            var jobExists = await db.PublishJobs.AnyAsync(j => j.Id == snapshot.PublishJobId);
            if (!jobExists) return Results.BadRequest("PublishJob not found.");

            snapshot.Id = Guid.NewGuid();
            snapshot.SnapshotDate = DateTime.UtcNow;

            db.InsightSnapshots.Add(snapshot);
            await db.SaveChangesAsync();

            return Results.Created($"/api/insights/{snapshot.Id}", snapshot);
        }).WithName("CreateInsightSnapshot");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var snapshot = await db.InsightSnapshots.FindAsync(id);
            if (snapshot is null) return Results.NotFound();

            db.InsightSnapshots.Remove(snapshot);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteInsightSnapshot");

        return group;
    }
}
