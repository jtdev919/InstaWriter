using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class ApprovalEndpoints
{
    public static RouteGroupBuilder MapApprovalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/approvals").WithTags("Approvals");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var approvals = await db.Approvals.OrderByDescending(a => a.Timestamp).ToListAsync();
            return Results.Ok(approvals);
        }).WithName("GetApprovals");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var approval = await db.Approvals.FindAsync(id);
            return approval is not null ? Results.Ok(approval) : Results.NotFound();
        }).WithName("GetApprovalById");

        group.MapGet("/by-draft/{draftId:guid}", async (Guid draftId, AppDbContext db) =>
        {
            var approvals = await db.Approvals
                .Where(a => a.ContentDraftId == draftId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
            return Results.Ok(approvals);
        }).WithName("GetApprovalsByDraft");

        group.MapPost("/", async (Approval approval, AppDbContext db) =>
        {
            var draftExists = await db.ContentDrafts.AnyAsync(d => d.Id == approval.ContentDraftId);
            if (!draftExists) return Results.BadRequest("ContentDraft not found.");

            approval.Id = Guid.NewGuid();
            approval.Timestamp = DateTime.UtcNow;

            db.Approvals.Add(approval);
            await db.SaveChangesAsync();

            return Results.Created($"/api/approvals/{approval.Id}", approval);
        }).WithName("CreateApproval");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var approval = await db.Approvals.FindAsync(id);
            if (approval is null) return Results.NotFound();

            db.Approvals.Remove(approval);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteApproval");

        return group;
    }
}
