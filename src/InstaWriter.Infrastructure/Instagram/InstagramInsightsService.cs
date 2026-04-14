using System.Net.Http.Json;
using System.Text.Json.Serialization;
using InstaWriter.Core.Services;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Instagram;

public class InstagramInsightsService(HttpClient httpClient, ILogger<InstagramInsightsService> logger) : IInsightsService
{
    private const string GraphApiBase = "https://graph.instagram.com/v22.0";

    public async Task<InsightsResult> FetchMediaInsightsAsync(string accessToken, string mediaId, CancellationToken ct = default)
    {
        try
        {
            // Step 1: Fetch public metrics from the media object itself
            logger.LogInformation("Fetching media fields for {MediaId}", mediaId);

            var mediaFields = await httpClient.GetFromJsonAsync<MediaFieldsResponse>(
                $"{GraphApiBase}/{mediaId}?fields=like_count,comments_count&access_token={accessToken}", ct);

            var likes = mediaFields?.LikeCount ?? 0;
            var comments = mediaFields?.CommentsCount ?? 0;

            // Step 2: Fetch insights metrics (reach, impressions, saves, shares, profile visits, follows)
            logger.LogInformation("Fetching insights for {MediaId}", mediaId);

            var insightsUrl = $"{GraphApiBase}/{mediaId}/insights?metric=reach,impressions,saved,shares,profile_visits,follows&access_token={accessToken}";
            var response = await httpClient.GetAsync(insightsUrl, ct);

            int reach = 0, views = 0, saves = 0, shares = 0, profileVisits = 0, follows = 0;

            if (response.IsSuccessStatusCode)
            {
                var insightsBody = await response.Content.ReadFromJsonAsync<InsightsResponse>(ct);

                if (insightsBody?.Data != null)
                {
                    foreach (var metric in insightsBody.Data)
                    {
                        var value = metric.Values?.FirstOrDefault()?.Value ?? 0;
                        switch (metric.Name)
                        {
                            case "reach": reach = value; break;
                            case "impressions": views = value; break;
                            case "saved": saves = value; break;
                            case "shares": shares = value; break;
                            case "profile_visits": profileVisits = value; break;
                            case "follows": follows = value; break;
                        }
                    }
                }
            }
            else
            {
                // Some media types don't support all insights — log but don't fail
                logger.LogWarning("Insights API returned {StatusCode} for {MediaId}, using partial data",
                    response.StatusCode, mediaId);
            }

            logger.LogInformation("Insights collected for {MediaId}: reach={Reach}, likes={Likes}, comments={Comments}, saves={Saves}, shares={Shares}",
                mediaId, reach, likes, comments, saves, shares);

            return new InsightsResult(true, reach, views, likes, comments, shares, saves, profileVisits, follows, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch insights for {MediaId}", mediaId);
            return new InsightsResult(false, 0, 0, 0, 0, 0, 0, 0, 0, ex.Message);
        }
    }

    private record MediaFieldsResponse
    {
        [JsonPropertyName("like_count")]
        public int LikeCount { get; init; }

        [JsonPropertyName("comments_count")]
        public int CommentsCount { get; init; }
    }

    private record InsightsResponse
    {
        [JsonPropertyName("data")]
        public List<InsightMetric>? Data { get; init; }
    }

    private record InsightMetric
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("values")]
        public List<InsightValue>? Values { get; init; }
    }

    private record InsightValue
    {
        [JsonPropertyName("value")]
        public int Value { get; init; }
    }
}
