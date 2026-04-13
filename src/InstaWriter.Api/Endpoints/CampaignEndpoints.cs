using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class CampaignEndpoints
{
    public static RouteGroupBuilder MapCampaignEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/campaigns").WithTags("Campaigns");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var campaigns = await db.Campaigns.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return Results.Ok(campaigns);
        }).WithName("GetCampaigns");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            return campaign is not null ? Results.Ok(campaign) : Results.NotFound();
        }).WithName("GetCampaignById");

        group.MapPost("/", async (Campaign campaign, AppDbContext db) =>
        {
            campaign.Id = Guid.NewGuid();
            campaign.CreatedAt = DateTime.UtcNow;

            db.Campaigns.Add(campaign);
            await db.SaveChangesAsync();

            return Results.Created($"/api/campaigns/{campaign.Id}", campaign);
        }).WithName("CreateCampaign");

        group.MapPut("/{id:guid}", async (Guid id, Campaign updated, AppDbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null) return Results.NotFound();

            campaign.Name = updated.Name;
            campaign.Objective = updated.Objective;
            campaign.StartDate = updated.StartDate;
            campaign.EndDate = updated.EndDate;
            campaign.Status = updated.Status;
            campaign.AudienceSegment = updated.AudienceSegment;
            campaign.KPISet = updated.KPISet;

            await db.SaveChangesAsync();
            return Results.Ok(campaign);
        }).WithName("UpdateCampaign");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var campaign = await db.Campaigns.FindAsync(id);
            if (campaign is null) return Results.NotFound();

            db.Campaigns.Remove(campaign);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteCampaign");

        return group;
    }
}
