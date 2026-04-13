using InstaWriter.Core.Services;

namespace InstaWriter.Api.Tests.Fakes;

public class FakeContentGenerator : IContentGenerator
{
    public Task<GeneratedDraft> GenerateDraftAsync(GenerateDraftRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new GeneratedDraft(
            Caption: $"AI-generated caption for: {request.IdeaTitle} \nBuilt with purpose. #health",
            Script: request.TargetFormat is "reel" or "reels" ? "Hook: Start with the problem.\nPoint 1...\nCTA: Follow for more." : null,
            HashtagSet: "#health #fitness #biomarkers #hrv #wellness #techfounder #buildinpublic",
            CoverText: "Your Data. Your Health."
        ));
    }

    public Task<string> RegenerateCaptionAsync(RegenerateCaptionRequest request, CancellationToken ct = default)
    {
        var direction = request.Direction ?? "default";
        return Task.FromResult($"Regenerated ({direction}): {request.CurrentCaption[..Math.Min(50, request.CurrentCaption.Length)]}...");
    }

    public Task<ComplianceResult> ScoreComplianceAsync(string caption, CancellationToken ct = default)
    {
        var hasRiskyWords = caption.Contains("cure", StringComparison.OrdinalIgnoreCase)
            || caption.Contains("treat", StringComparison.OrdinalIgnoreCase)
            || caption.Contains("diagnose", StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(hasRiskyWords
            ? new ComplianceResult(0.8, "High", ["Contains medical claim language"], "Consider using softer language.")
            : new ComplianceResult(0.1, "Low", [], null));
    }
}
