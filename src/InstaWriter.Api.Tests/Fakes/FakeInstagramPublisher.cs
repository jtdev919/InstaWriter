using InstaWriter.Core.Services;

namespace InstaWriter.Api.Tests.Fakes;

public class FakeInstagramPublisher : IInstagramPublisher
{
    public bool ShouldSucceed { get; set; } = true;
    public string FakeMediaId { get; set; } = "fake_media_123";
    public string FakeContainerId { get; set; } = "fake_container_456";

    public Task<PublishResult> PublishSingleImageAsync(
        string accessToken, string igUserId, string imageUrl, string caption, CancellationToken ct = default)
    {
        return Task.FromResult(ShouldSucceed
            ? new PublishResult(true, FakeContainerId, FakeMediaId, null)
            : new PublishResult(false, FakeContainerId, null, "Fake publish error"));
    }

    public Task<PublishResult> PublishReelAsync(
        string accessToken, string igUserId, string videoUrl, string caption, CancellationToken ct = default)
    {
        return Task.FromResult(ShouldSucceed
            ? new PublishResult(true, FakeContainerId, FakeMediaId, null)
            : new PublishResult(false, FakeContainerId, null, "Fake publish error"));
    }

    public Task<PublishResult> PublishCarouselAsync(
        string accessToken, string igUserId, IReadOnlyList<CarouselItem> items, string caption, CancellationToken ct = default)
    {
        return Task.FromResult(ShouldSucceed
            ? new PublishResult(true, FakeContainerId, FakeMediaId, null)
            : new PublishResult(false, FakeContainerId, null, "Fake carousel publish error"));
    }
}
