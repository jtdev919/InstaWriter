namespace InstaWriter.Core.Services;

public interface IInsightsService
{
    Task<InsightsResult> FetchMediaInsightsAsync(string accessToken, string mediaId, CancellationToken ct = default);
}

public record InsightsResult(
    bool Success,
    int Reach,
    int Views,
    int Likes,
    int Comments,
    int Shares,
    int Saves,
    int ProfileVisits,
    int FollowsAttributed,
    string? Error);
