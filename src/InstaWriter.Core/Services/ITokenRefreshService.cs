namespace InstaWriter.Core.Services;

public interface ITokenRefreshService
{
    Task<TokenRefreshResult> RefreshLongLivedTokenAsync(string currentToken, CancellationToken ct = default);
}

public record TokenRefreshResult(
    bool Success,
    string? NewToken,
    DateTime? ExpiresAt,
    string? Error);
