namespace InstaWriter.Core.Services;

public interface IInstagramPublisher
{
    Task<PublishResult> PublishSingleImageAsync(
        string accessToken,
        string igUserId,
        string imageUrl,
        string caption,
        CancellationToken ct = default);

    Task<PublishResult> PublishReelAsync(
        string accessToken,
        string igUserId,
        string videoUrl,
        string caption,
        CancellationToken ct = default);

    Task<PublishResult> PublishCarouselAsync(
        string accessToken,
        string igUserId,
        IReadOnlyList<CarouselItem> items,
        string caption,
        CancellationToken ct = default);
}

public record CarouselItem(string MediaUrl, CarouselItemType ItemType);

public enum CarouselItemType
{
    Image,
    Video
}

public record PublishResult(
    bool Success,
    string? ContainerId,
    string? MediaId,
    string? Error);
