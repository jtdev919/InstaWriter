using InstaWriter.Core.Services;

namespace InstaWriter.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static RouteGroupBuilder MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics");

        group.MapGet("/posts/top", async (IPerformanceAnalyticsService analytics, int? count) =>
        {
            var results = await analytics.GetTopPostsAsync(count ?? 10);
            return Results.Ok(results);
        }).WithName("GetTopPosts");

        group.MapGet("/posts/{jobId:guid}/score", async (Guid jobId, IPerformanceAnalyticsService analytics) =>
        {
            try
            {
                var score = await analytics.ScorePostAsync(jobId);
                return Results.Ok(score);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(new { Error = ex.Message });
            }
        }).WithName("ScorePost");

        group.MapGet("/clusters", async (IPerformanceAnalyticsService analytics) =>
        {
            var clusters = await analytics.GetPerformanceClustersAsync();
            return Results.Ok(clusters);
        }).WithName("GetPerformanceClusters");

        group.MapGet("/cta-insights", async (IPerformanceAnalyticsService analytics) =>
        {
            var insights = await analytics.GetCTAInsightsAsync();
            return Results.Ok(insights);
        }).WithName("GetCTAInsights");

        group.MapGet("/pillars/performance", async (IPerformanceAnalyticsService analytics) =>
        {
            var performance = await analytics.GetPillarPerformanceAsync();
            return Results.Ok(performance);
        }).WithName("GetPillarPerformance");

        group.MapPost("/pillars/update-weights", async (IPerformanceAnalyticsService analytics) =>
        {
            await analytics.UpdatePillarWeightsAsync();
            return Results.Ok(new { Message = "Pillar weights updated based on performance data." });
        }).WithName("UpdatePillarWeights");

        group.MapGet("/recommendations", async (IPerformanceAnalyticsService analytics, int? count) =>
        {
            var recommendations = await analytics.GetRecommendationsAsync(count ?? 5);
            return Results.Ok(recommendations);
        }).WithName("GetRecommendations");

        return group;
    }
}
