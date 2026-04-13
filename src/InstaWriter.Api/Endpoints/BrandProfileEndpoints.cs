using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class BrandProfileEndpoints
{
    public static RouteGroupBuilder MapBrandProfileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/brand-profiles").WithTags("BrandProfiles");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var profiles = await db.BrandProfiles.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return Results.Ok(profiles);
        }).WithName("GetBrandProfiles");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var profile = await db.BrandProfiles.FindAsync(id);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        }).WithName("GetBrandProfileById");

        group.MapPost("/", async (BrandProfile profile, AppDbContext db) =>
        {
            profile.Id = Guid.NewGuid();
            profile.CreatedAt = DateTime.UtcNow;

            db.BrandProfiles.Add(profile);
            await db.SaveChangesAsync();

            return Results.Created($"/api/brand-profiles/{profile.Id}", profile);
        }).WithName("CreateBrandProfile");

        group.MapPut("/{id:guid}", async (Guid id, BrandProfile updated, AppDbContext db) =>
        {
            var profile = await db.BrandProfiles.FindAsync(id);
            if (profile is null) return Results.NotFound();

            profile.Name = updated.Name;
            profile.VoiceGuide = updated.VoiceGuide;
            profile.ToneGuide = updated.ToneGuide;
            profile.CTAStyle = updated.CTAStyle;
            profile.DisclaimerRules = updated.DisclaimerRules;
            profile.DefaultHashtagSets = updated.DefaultHashtagSets;
            profile.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(profile);
        }).WithName("UpdateBrandProfile");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var profile = await db.BrandProfiles.FindAsync(id);
            if (profile is null) return Results.NotFound();

            db.BrandProfiles.Remove(profile);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteBrandProfile");

        return group;
    }
}
