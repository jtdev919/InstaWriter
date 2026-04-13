using System.Net.Http.Json;
using System.Text.Json.Serialization;
using InstaWriter.Core.Services;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Instagram;

public class MetaTokenRefreshService(HttpClient httpClient, ILogger<MetaTokenRefreshService> logger) : ITokenRefreshService
{
    private const string GraphApiBase = "https://graph.instagram.com";

    public async Task<TokenRefreshResult> RefreshLongLivedTokenAsync(string currentToken, CancellationToken ct = default)
    {
        try
        {
            // Instagram long-lived tokens can be refreshed via GET with grant_type=ig_refresh_token
            // This works for tokens that are at least 24 hours old and not yet expired
            var url = $"{GraphApiBase}/refresh_access_token?grant_type=ig_refresh_token&access_token={currentToken}";

            logger.LogInformation("Attempting to refresh Instagram long-lived token");

            var response = await httpClient.GetAsync(url, ct);
            var body = await response.Content.ReadFromJsonAsync<TokenRefreshResponse>(ct);

            if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(body?.AccessToken))
            {
                var error = body?.Error?.Message ?? $"Token refresh failed with status {response.StatusCode}";
                logger.LogError("Token refresh failed: {Error}", error);
                return new TokenRefreshResult(false, null, null, error);
            }

            var expiresAt = DateTime.UtcNow.AddSeconds(body.ExpiresIn);

            logger.LogInformation("Token refreshed successfully, expires at {ExpiresAt}", expiresAt);
            return new TokenRefreshResult(true, body.AccessToken, expiresAt, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token refresh failed with exception");
            return new TokenRefreshResult(false, null, null, ex.Message);
        }
    }

    private record TokenRefreshResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; init; }

        [JsonPropertyName("error")]
        public TokenRefreshError? Error { get; init; }
    }

    private record TokenRefreshError
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("code")]
        public int? Code { get; init; }
    }
}
