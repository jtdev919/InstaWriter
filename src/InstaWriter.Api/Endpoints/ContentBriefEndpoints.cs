using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Carousel;
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

        group.MapPost("/{id:guid}/fallback", async (Guid id, IFallbackSubstitutionService fallback) =>
        {
            var result = await fallback.AttemptFallbackAsync(id);

            if (!result.Success)
                return Results.BadRequest(new { Error = result.Reason });

            return Results.Ok(new
            {
                result.SubstitutedAssetId,
                result.CreatedDraftId,
                Message = "Fallback asset substituted successfully."
            });
        }).WithName("FallbackSubstitution");

        group.MapPost("/{id:guid}/render-carousel", async (Guid id, AppDbContext db, ICarouselRenderer renderer, IBlobStorageService blobStorage) =>
        {
            var brief = await db.ContentBriefs
                .Include(b => b.ContentIdea)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brief is null) return Results.NotFound();

            // Compose slide payload from the brief
            var author = brief.ContentIdea?.PillarName ?? "InstaWriter";
            var request = CarouselCompositionService.ComposeFromBrief(brief, $"@{author}");

            // Render slides
            var renderedSlides = await renderer.RenderCarouselAsync(request);

            // Save each slide as an Asset
            var assetIds = new List<Guid>();
            foreach (var slide in renderedSlides)
            {
                using var stream = new MemoryStream(slide.PngData);
                var uploadResult = await blobStorage.UploadAsync(slide.FileName, "image/png", stream);

                var asset = new Asset
                {
                    Id = Guid.NewGuid(),
                    FileName = slide.FileName,
                    ContentType = "image/png",
                    FileSizeBytes = uploadResult.FileSizeBytes,
                    BlobUri = uploadResult.Uri,
                    AssetType = AssetType.Carousel,
                    Status = AssetStatus.Ready,
                    PillarName = brief.ContentIdea?.PillarName,
                    ContentIdeaId = brief.ContentIdeaId,
                    Tags = $"carousel,slide-{slide.PageNumber}",
                    CreatedAt = DateTime.UtcNow
                };

                db.Assets.Add(asset);
                assetIds.Add(asset.Id);
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                BriefId = brief.Id,
                SlideCount = renderedSlides.Count,
                AssetIds = assetIds,
                Message = $"Rendered {renderedSlides.Count} carousel slides."
            });
        }).WithName("RenderCarousel");

        return group;
    }
}
