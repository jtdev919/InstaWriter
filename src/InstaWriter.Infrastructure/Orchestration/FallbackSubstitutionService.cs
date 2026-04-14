using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Orchestration;

public class FallbackSubstitutionService(AppDbContext db, ILogger<FallbackSubstitutionService> logger) : IFallbackSubstitutionService
{
    public async Task<FallbackResult> AttemptFallbackAsync(Guid contentBriefId, CancellationToken ct = default)
    {
        var brief = await db.ContentBriefs
            .Include(b => b.ContentIdea)
            .FirstOrDefaultAsync(b => b.Id == contentBriefId, ct);

        if (brief is null)
            return new FallbackResult(false, null, null, "ContentBrief not found.");

        if (!brief.RequiresOriginalMedia)
            return new FallbackResult(false, null, null, "Brief does not require original media — fallback not applicable.");

        // Find a suitable fallback asset from the library
        // Priority: match by pillar name, then by content idea, then any Ready asset
        var pillarName = brief.ContentIdea?.PillarName;

        Asset? fallbackAsset = null;

        // Try to find an asset matching the pillar
        if (!string.IsNullOrEmpty(pillarName))
        {
            fallbackAsset = await db.Assets
                .Where(a => a.Status == AssetStatus.Ready && a.PillarName == pillarName)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        // Try to find an asset linked to the same content idea
        if (fallbackAsset is null)
        {
            fallbackAsset = await db.Assets
                .Where(a => a.Status == AssetStatus.Ready && a.ContentIdeaId == brief.ContentIdeaId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        // Last resort: any Ready image asset
        if (fallbackAsset is null)
        {
            fallbackAsset = await db.Assets
                .Where(a => a.Status == AssetStatus.Ready && a.AssetType == AssetType.Image)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        if (fallbackAsset is null)
            return new FallbackResult(false, null, null, "No suitable fallback asset found in the library.");

        logger.LogInformation("Found fallback asset {AssetId} ({FileName}) for brief {BriefId}",
            fallbackAsset.Id, fallbackAsset.FileName, contentBriefId);

        // Link the fallback asset to the content idea
        if (fallbackAsset.ContentIdeaId is null)
        {
            fallbackAsset.ContentIdeaId = brief.ContentIdeaId;
            await db.SaveChangesAsync(ct);
        }

        // Check if there's already a draft for this brief
        var existingDraft = await db.ContentDrafts
            .FirstOrDefaultAsync(d => d.ContentBriefId == contentBriefId, ct);

        Guid? createdDraftId = null;

        if (existingDraft is null)
        {
            // Create a draft from the brief using the fallback asset
            var draft = new ContentDraft
            {
                Id = Guid.NewGuid(),
                ContentIdeaId = brief.ContentIdeaId,
                ContentBriefId = brief.Id,
                Caption = $"[Fallback media used] {brief.KeyMessage}",
                CoverText = brief.HookDirection,
                Status = ContentDraftStatus.Draft,
                VersionNo = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.ContentDrafts.Add(draft);
            createdDraftId = draft.Id;

            logger.LogInformation("Created fallback draft {DraftId} for brief {BriefId} using asset {AssetId}",
                draft.Id, contentBriefId, fallbackAsset.Id);
        }

        // Cancel overdue original-media tasks for this brief's idea
        var overdueTasks = await db.TaskItems
            .Where(t => t.RelatedEntityType == "ContentBrief"
                && t.RelatedEntityId == contentBriefId
                && (t.Status == TaskItemStatus.Pending || t.Status == TaskItemStatus.InProgress || t.Status == TaskItemStatus.Overdue))
            .ToListAsync(ct);

        foreach (var task in overdueTasks)
        {
            task.Status = TaskItemStatus.Cancelled;
            logger.LogInformation("Cancelled overdue task {TaskId} ({TaskType}) due to fallback substitution", task.Id, task.TaskType);
        }

        // Log the substitution event
        var wfEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            EventType = "FallbackSubstitution",
            EntityType = "ContentBrief",
            EntityId = contentBriefId,
            EventTime = DateTime.UtcNow,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                fallbackAssetId = fallbackAsset.Id,
                fallbackAssetFileName = fallbackAsset.FileName,
                createdDraftId,
                cancelledTasks = overdueTasks.Select(t => t.Id).ToList()
            })
        };
        db.WorkflowEvents.Add(wfEvent);

        await db.SaveChangesAsync(ct);

        return new FallbackResult(true, fallbackAsset.Id, createdDraftId, null);
    }
}
