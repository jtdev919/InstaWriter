using InstaWriter.Core.Services;

namespace InstaWriter.Api.Tests.Fakes;

public class FakeTokenRefreshService : ITokenRefreshService
{
    public bool ShouldSucceed { get; set; } = true;
    public string FakeNewToken { get; set; } = "refreshed_token_abc";
    public DateTime FakeExpiresAt { get; set; } = DateTime.UtcNow.AddDays(60);

    public Task<TokenRefreshResult> RefreshLongLivedTokenAsync(string currentToken, CancellationToken ct = default)
    {
        return Task.FromResult(ShouldSucceed
            ? new TokenRefreshResult(true, FakeNewToken, FakeExpiresAt, null)
            : new TokenRefreshResult(false, null, null, "Fake refresh error"));
    }
}
