using System.Net.Http.Json;
using System.Text.Json.Serialization;
using InstaWriter.Core.Services;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Instagram;

public class InstagramPublisher(HttpClient httpClient, ILogger<InstagramPublisher> logger) : IInstagramPublisher
{
    // Instagram Graph API base URL
    private const string GraphApiBase = "https://graph.instagram.com/v22.0";

    public async Task<PublishResult> PublishSingleImageAsync(
        string accessToken, string igUserId, string imageUrl, string caption, CancellationToken ct = default)
    {
        return await PublishMediaAsync(accessToken, igUserId, new Dictionary<string, string>
        {
            ["image_url"] = imageUrl,
            ["caption"] = caption,
            ["access_token"] = accessToken
        }, ct);
    }

    public async Task<PublishResult> PublishReelAsync(
        string accessToken, string igUserId, string videoUrl, string caption, CancellationToken ct = default)
    {
        return await PublishMediaAsync(accessToken, igUserId, new Dictionary<string, string>
        {
            ["video_url"] = videoUrl,
            ["caption"] = caption,
            ["media_type"] = "REELS",
            ["access_token"] = accessToken
        }, ct);
    }

    private async Task<PublishResult> PublishMediaAsync(
        string accessToken, string igUserId, Dictionary<string, string> containerParams, CancellationToken ct)
    {
        try
        {
            // Step 1: Create media container
            logger.LogInformation("Creating media container for IG user {UserId}", igUserId);

            var containerResponse = await httpClient.PostAsync(
                $"{GraphApiBase}/{igUserId}/media",
                new FormUrlEncodedContent(containerParams),
                ct);

            var containerBody = await containerResponse.Content.ReadFromJsonAsync<GraphApiResponse>(ct);

            if (!containerResponse.IsSuccessStatusCode || containerBody?.Id is null)
            {
                var error = containerBody?.Error?.Message ?? $"Container creation failed with status {containerResponse.StatusCode}";
                logger.LogError("Container creation failed: {Error}", error);
                return new PublishResult(false, null, null, error);
            }

            var containerId = containerBody.Id;
            logger.LogInformation("Container created: {ContainerId}", containerId);

            // Step 2: Wait for container to be ready (videos need processing time)
            if (containerParams.ContainsKey("video_url"))
            {
                var ready = await WaitForContainerReady(accessToken, containerId, ct);
                if (!ready)
                    return new PublishResult(false, containerId, null, "Container did not become ready within timeout");
            }

            // Step 3: Publish the container
            logger.LogInformation("Publishing container {ContainerId}", containerId);

            var publishResponse = await httpClient.PostAsync(
                $"{GraphApiBase}/{igUserId}/media_publish",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["creation_id"] = containerId,
                    ["access_token"] = accessToken
                }),
                ct);

            var publishBody = await publishResponse.Content.ReadFromJsonAsync<GraphApiResponse>(ct);

            if (!publishResponse.IsSuccessStatusCode || publishBody?.Id is null)
            {
                var error = publishBody?.Error?.Message ?? $"Publish failed with status {publishResponse.StatusCode}";
                logger.LogError("Publish failed: {Error}", error);
                return new PublishResult(false, containerId, null, error);
            }

            logger.LogInformation("Published successfully. Media ID: {MediaId}", publishBody.Id);
            return new PublishResult(true, containerId, publishBody.Id, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Instagram publish failed");
            return new PublishResult(false, null, null, ex.Message);
        }
    }

    private async Task<bool> WaitForContainerReady(string accessToken, string containerId, CancellationToken ct)
    {
        // Poll container status — videos need processing time
        for (var i = 0; i < 30; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            var statusResponse = await httpClient.GetFromJsonAsync<ContainerStatusResponse>(
                $"{GraphApiBase}/{containerId}?fields=status_code&access_token={accessToken}", ct);

            if (statusResponse?.StatusCode == "FINISHED")
                return true;

            if (statusResponse?.StatusCode == "ERROR")
            {
                logger.LogError("Container {ContainerId} entered ERROR state", containerId);
                return false;
            }

            logger.LogDebug("Container {ContainerId} status: {Status}", containerId, statusResponse?.StatusCode);
        }

        return false;
    }

    private record GraphApiResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("error")]
        public GraphApiError? Error { get; init; }
    }

    private record GraphApiError
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("code")]
        public int? Code { get; init; }
    }

    private record ContainerStatusResponse
    {
        [JsonPropertyName("status_code")]
        public string? StatusCode { get; init; }
    }
}
