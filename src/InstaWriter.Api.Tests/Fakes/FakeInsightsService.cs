using InstaWriter.Core.Services;

namespace InstaWriter.Api.Tests.Fakes;

public class FakeInsightsService : IInsightsService
{
    public bool ShouldSucceed { get; set; } = true;
    public int FakeReach { get; set; } = 1500;
    public int FakeViews { get; set; } = 3200;
    public int FakeLikes { get; set; } = 120;
    public int FakeComments { get; set; } = 15;
    public int FakeShares { get; set; } = 8;
    public int FakeSaves { get; set; } = 45;
    public int FakeProfileVisits { get; set; } = 30;
    public int FakeFollows { get; set; } = 5;

    public Task<InsightsResult> FetchMediaInsightsAsync(string accessToken, string mediaId, CancellationToken ct = default)
    {
        return Task.FromResult(ShouldSucceed
            ? new InsightsResult(true, FakeReach, FakeViews, FakeLikes, FakeComments, FakeShares, FakeSaves, FakeProfileVisits, FakeFollows, null)
            : new InsightsResult(false, 0, 0, 0, 0, 0, 0, 0, 0, "Fake insights error"));
    }
}
