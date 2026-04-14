namespace InstaWriter.Core.Services;

public interface IFallbackSubstitutionService
{
    Task<FallbackResult> AttemptFallbackAsync(Guid contentBriefId, CancellationToken ct = default);
}

public record FallbackResult(
    bool Success,
    Guid? SubstitutedAssetId,
    Guid? CreatedDraftId,
    string? Reason);
