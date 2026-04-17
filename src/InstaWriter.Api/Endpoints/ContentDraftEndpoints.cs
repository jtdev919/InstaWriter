using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Core.Workflow;
using InstaWriter.Infrastructure.Carousel;
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

        group.MapPost("/{id:guid}/transition", async (Guid id, TransitionRequest request, AppDbContext db, IOrchestrationService orchestration) =>
        {
            var draft = await db.ContentDrafts.FindAsync(id);
            if (draft is null) return Results.NotFound();

            if (!Enum.TryParse<ContentDraftStatus>(request.Status, true, out var newStatus))
                return Results.BadRequest(new { Error = $"Invalid status '{request.Status}'.", Allowed = StatusTransitions.AllowedTransitions(draft.Status) });

            if (!StatusTransitions.CanTransition(draft.Status, newStatus))
                return Results.BadRequest(new { Error = $"Cannot transition from '{draft.Status}' to '{newStatus}'.", Allowed = StatusTransitions.AllowedTransitions(draft.Status) });

            var fromStatus = draft.Status;
            draft.Status = newStatus;
            await db.SaveChangesAsync();

            await orchestration.OnContentDraftTransitionAsync(draft, fromStatus);

            return Results.Ok(draft);
        }).WithName("TransitionContentDraft");

        group.MapPost("/{id:guid}/render-carousel", async (Guid id, AppDbContext db, ICarouselRenderer renderer, IBlobStorageService blobStorage) =>
        {
            var draft = await db.ContentDrafts
                .Include(d => d.ContentIdea)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (draft is null) return Results.NotFound();

            var category = draft.ContentIdea?.PillarName?.ToUpperInvariant() ?? "HEALTH & PERFORMANCE";
            var author = "@josephtolandsr";

            // Split caption into meaningful chunks for slides
            var captionLines = draft.Caption
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l => !l.StartsWith('#'))
                .ToList();

            var title = draft.ContentIdea?.Title ?? draft.CoverText ?? "Swipe to learn more";
            var bodyChunks = new List<string>();
            var currentChunk = "";
            foreach (var line in captionLines)
            {
                if ((currentChunk + " " + line).Length > 200 && currentChunk.Length > 0)
                {
                    bodyChunks.Add(currentChunk.Trim());
                    currentChunk = line;
                }
                else
                {
                    currentChunk = string.IsNullOrEmpty(currentChunk) ? line : currentChunk + " " + line;
                }
            }
            if (!string.IsNullOrEmpty(currentChunk))
                bodyChunks.Add(currentChunk.Trim());

            // Ensure we have enough content for slides
            while (bodyChunks.Count < 5)
                bodyChunks.Add("");

            var slides = new List<SlideData>
            {
                new("title", Headline: title, Subtext: "Swipe to learn more", Category: category, SlideNumber: 1),
                new("content", Headline: bodyChunks.Count > 0 ? bodyChunks[0].Split('.')[0] : "The Story", Body: bodyChunks.ElementAtOrDefault(0) ?? "", Category: category, SlideNumber: 2),
                new("content", Headline: bodyChunks.Count > 1 ? bodyChunks[1].Split('.')[0] : "Key Insight", Body: bodyChunks.ElementAtOrDefault(1) ?? "", Category: category, SlideNumber: 3),
                new("content", Headline: bodyChunks.Count > 2 ? bodyChunks[2].Split('.')[0] : "Why It Matters", Body: bodyChunks.ElementAtOrDefault(2) ?? "", Category: category, SlideNumber: 4),
                new("content", Headline: bodyChunks.Count > 3 ? bodyChunks[3].Split('.')[0] : "Take Action", Body: bodyChunks.ElementAtOrDefault(3) ?? "", Category: category, SlideNumber: 5),
                new("content", Headline: bodyChunks.Count > 4 ? bodyChunks[4].Split('.')[0] : "The Difference", Body: bodyChunks.ElementAtOrDefault(4) ?? "", Category: category, SlideNumber: 6),
                new("cta-bridge", Headline: "Follow for more", Body: "Biohacking tips, build updates, and health insights from my own journey."),
                new("cta", Headline: "Link in bio", CTA: "Get Started Free", Subtext: draft.HashtagSet ?? ""),
            };

            var request = new CarouselRenderRequest("educational", slides, author);
            var renderedSlides = await renderer.RenderCarouselAsync(request);

            var assetIds = new List<Guid>();
            var assetUrls = new List<string>();
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
                    Tags = $"carousel,slide-{slide.PageNumber}",
                    ContentDraftId = draft.Id,
                    CreatedAt = DateTime.UtcNow
                };

                db.Assets.Add(asset);
                assetIds.Add(asset.Id);
                assetUrls.Add(uploadResult.Uri);
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { DraftId = id, SlideCount = renderedSlides.Count, AssetIds = assetIds, AssetUrls = assetUrls });
        }).WithName("RenderCarouselFromDraft");

        group.MapGet("/{id:guid}/carousel-assets", async (Guid id, AppDbContext db) =>
        {
            var assets = await db.Assets
                .Where(a => a.ContentDraftId == id && a.AssetType == AssetType.Carousel)
                .OrderBy(a => a.Tags)
                .ToListAsync();
            return Results.Ok(assets);
        }).WithName("GetCarouselAssets");

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
