namespace InstaWriter.Core.Services;

public interface IContentGenerator
{
    Task<GeneratedDraft> GenerateDraftAsync(GenerateDraftRequest request, CancellationToken ct = default);
    Task<string> RegenerateCaptionAsync(RegenerateCaptionRequest request, CancellationToken ct = default);
    Task<ComplianceResult> ScoreComplianceAsync(string caption, CancellationToken ct = default);
}

public record GenerateDraftRequest(
    string IdeaTitle,
    string? IdeaSummary,
    string? PillarName,
    string? TargetFormat // e.g. "carousel", "reel", "single_image"
);

public record GeneratedDraft(
    string Caption,
    string? Script,
    string HashtagSet,
    string? CoverText
);

public record RegenerateCaptionRequest(
    string CurrentCaption,
    string? Direction // e.g. "shorter", "more casual", "add CTA", "emphasize community"
);

public record ComplianceResult(
    double Score,
    string RiskLevel,
    string[] Flags,
    string? SuggestedRewrite
);
