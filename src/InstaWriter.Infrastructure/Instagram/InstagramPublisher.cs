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

    public async Task<PublishResult> PublishCarouselAsync(
        string accessToken, string igUserId, IReadOnlyList<CarouselItem> items, string caption, CancellationToken ct = default)
    {
        if (items.Count < 2 || items.Count > 10)
            return new PublishResult(false, null, null, "Carousel requires between 2 and 10 items.");

        try
        {
            // Step 1: Create individual item containers
            var childIds = new List<string>();

            foreach (var item in items)
            {
                logger.LogInformation("Creating carousel item container ({Type})", item.ItemType);

                var containerParams = new Dictionary<string, string>
                {
                    ["is_carousel_item"] = "true",
                    ["access_token"] = accessToken
                };

                if (item.ItemType == CarouselItemType.Video)
                {
                    containerParams["video_url"] = item.MediaUrl;
                    containerParams["media_type"] = "VIDEO";
                }
                else
                {
                    containerParams["image_url"] = item.MediaUrl;
                }

                var response = await httpClient.PostAsync(
                    $"{GraphApiBase}/{igUserId}/media",
                    new FormUrlEncodedContent(containerParams),
                    ct);

                var body = await response.Content.ReadFromJsonAsync<GraphApiResponse>(ct);

                if (!response.IsSuccessStatusCode || body?.Id is null)
                {
                    var error = body?.Error?.Message ?? $"Carousel item container creation failed with status {response.StatusCode}";
                    logger.LogError("Carousel item container failed: {Error}", error);
                    return new PublishResult(false, null, null, error);
                }

                // Wait for video items to finish processing
                if (item.ItemType == CarouselItemType.Video)
                {
                    var ready = await WaitForContainerReady(accessToken, body.Id, ct);
                    if (!ready)
                        return new PublishResult(false, body.Id, null, $"Carousel video item {body.Id} did not become ready within timeout");
                }

                childIds.Add(body.Id);
                logger.LogInformation("Carousel item container created: {ContainerId}", body.Id);
            }

            // Step 2: Create carousel container referencing all children
            logger.LogInformation("Creating carousel container with {Count} items", childIds.Count);

            var carouselParams = new Dictionary<string, string>
            {
                ["media_type"] = "CAROUSEL",
                ["caption"] = caption,
                ["children"] = string.Join(",", childIds),
                ["access_token"] = accessToken
            };

            var carouselResponse = await httpClient.PostAsync(
                $"{GraphApiBase}/{igUserId}/media",
                new FormUrlEncodedContent(carouselParams),
                ct);

            var carouselBody = await carouselResponse.Content.ReadFromJsonAsync<GraphApiResponse>(ct);

            if (!carouselResponse.IsSuccessStatusCode || carouselBody?.Id is null)
            {
                var error = carouselBody?.Error?.Message ?? $"Carousel container creation failed with status {carouselResponse.StatusCode}";
                logger.LogError("Carousel container creation failed: {Error}", error);
                return new PublishResult(false, null, null, error);
            }

            var carouselContainerId = carouselBody.Id;
            logger.LogInformation("Carousel container created: {ContainerId}", carouselContainerId);

            // Step 3: Publish the carousel
            logger.LogInformation("Publishing carousel {ContainerId}", carouselContainerId);

            var publishResponse = await httpClient.PostAsync(
                $"{GraphApiBase}/{igUserId}/media_publish",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["creation_id"] = carouselContainerId,
                    ["access_token"] = accessToken
                }),
                ct);

            var publishBody = await publishResponse.Content.ReadFromJsonAsync<GraphApiResponse>(ct);

            if (!publishResponse.IsSuccessStatusCode || publishBody?.Id is null)
            {
                var error = publishBody?.Error?.Message ?? $"Carousel publish failed with status {publishResponse.StatusCode}";
                logger.LogError("Carousel publish failed: {Error}", error);
                return new PublishResult(false, carouselContainerId, null, error);
            }

            logger.LogInformation("Carousel published successfully. Media ID: {MediaId}", publishBody.Id);
            return new PublishResult(true, carouselContainerId, publishBody.Id, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Instagram carousel publish failed");
            return new PublishResult(false, null, null, ex.Message);
        }
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
