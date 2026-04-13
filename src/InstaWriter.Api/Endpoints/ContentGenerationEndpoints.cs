using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class ContentGenerationEndpoints
{
    public static RouteGroupBuilder MapContentGenerationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/content").WithTags("Content Generation");

        group.MapPost("/drafts/generate", async (GenerateDraftFromIdeaRequest request, AppDbContext db, IContentGenerator generator) =>
        {
            var idea = await db.ContentIdeas.FindAsync(request.ContentIdeaId);
            if (idea is null)
                return Results.BadRequest(new { Error = "ContentIdeaId not found." });

            var generated = await generator.GenerateDraftAsync(new GenerateDraftRequest(
                idea.Title,
                idea.Summary,
                idea.PillarName,
                request.TargetFormat
            ));

            var draft = new ContentDraft
            {
                Id = Guid.NewGuid(),
                ContentIdeaId = idea.Id,
                Caption = generated.Caption,
                Script = generated.Script,
                HashtagSet = generated.HashtagSet,
                CoverText = generated.CoverText,
                Status = ContentDraftStatus.Draft,
                VersionNo = 1,
                CreatedAt = DateTime.UtcNow
            };

            db.ContentDrafts.Add(draft);
            await db.SaveChangesAsync();

            return Results.Created($"/api/content/drafts/{draft.Id}", draft);
        }).WithName("GenerateDraftFromIdea");

        group.MapPost("/drafts/{id:guid}/regenerate-caption", async (Guid id, RegenerateCaptionApiRequest? request, AppDbContext db, IContentGenerator generator) =>
        {
            var draft = await db.ContentDrafts.FindAsync(id);
            if (draft is null) return Results.NotFound();

            var newCaption = await generator.RegenerateCaptionAsync(new RegenerateCaptionRequest(
                draft.Caption,
                request?.Direction
            ));

            draft.Caption = newCaption;
            draft.VersionNo++;
            await db.SaveChangesAsync();

            return Results.Ok(draft);
        }).WithName("RegenerateDraftCaption");

        group.MapPost("/drafts/{id:guid}/score-compliance", async (Guid id, AppDbContext db, IContentGenerator generator) =>
        {
            var draft = await db.ContentDrafts.FindAsync(id);
            if (draft is null) return Results.NotFound();

            var result = await generator.ScoreComplianceAsync(draft.Caption);

            draft.ComplianceScore = result.Score;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                DraftId = draft.Id,
                result.Score,
                result.RiskLevel,
                result.Flags,
                result.SuggestedRewrite,
                draft.Caption
            });
        }).WithName("ScoreDraftCompliance");

        return group;
    }

    public record GenerateDraftFromIdeaRequest(Guid ContentIdeaId, string? TargetFormat);
    public record RegenerateCaptionApiRequest(string? Direction);
}
