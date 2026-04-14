namespace InstaWriter.Core.Services;

public interface IPerformanceAnalyticsService
{
    Task<PostScore> ScorePostAsync(Guid publishJobId, CancellationToken ct = default);
    Task<List<PostScore>> GetTopPostsAsync(int count = 10, CancellationToken ct = default);
    Task<List<PerformanceCluster>> GetPerformanceClustersAsync(CancellationToken ct = default);
    Task<List<CTAInsight>> GetCTAInsightsAsync(CancellationToken ct = default);
    Task<List<PillarPerformance>> GetPillarPerformanceAsync(CancellationToken ct = default);
    Task UpdatePillarWeightsAsync(CancellationToken ct = default);
    Task<List<PostRecommendation>> GetRecommendationsAsync(int count = 5, CancellationToken ct = default);
}

public record PostScore(
    Guid PublishJobId,
    Guid ContentDraftId,
    string? PillarName,
    string? TargetFormat,
    double EngagementScore,
    int Reach,
    int TotalEngagements,
    double EngagementRate);

public record PerformanceCluster(
    string GroupKey,
    string GroupType,  // "Format", "Pillar", "RiskLevel"
    int PostCount,
    double AvgEngagementScore,
    double AvgReach,
    double AvgEngagementRate);

public record CTAInsight(
    string CTAPattern,
    int PostCount,
    double AvgEngagementRate,
    double AvgSaves,
    double AvgShares);

public record PillarPerformance(
    string PillarName,
    int PostCount,
    double AvgEngagementScore,
    double CurrentWeight,
    double RecommendedWeight);

public record PostRecommendation(
    string PillarName,
    string SuggestedFormat,
    string Rationale,
    double ConfidenceScore);
