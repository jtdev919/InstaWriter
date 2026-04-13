using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class ChannelAccountTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask PostChannel_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var account = new ChannelAccount
        {
            AccountName = "test_ig_account",
            ExternalAccountId = "12345678"
        };

        var response = await _client.PostAsJsonAsync("/api/channels", account, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ChannelAccount>(ct);
        Assert.NotNull(created);
        Assert.Equal(AuthStatus.Pending, created.AuthStatus);
        Assert.Null(created.AccessToken); // Token should never be returned
    }

    [Fact]
    public async ValueTask PostChannel_WithToken_SetsConnected()
    {
        var ct = TestContext.Current.CancellationToken;
        var account = new ChannelAccount
        {
            AccountName = "connected_account",
            ExternalAccountId = "87654321",
            AccessToken = "some_valid_token"
        };

        var response = await _client.PostAsJsonAsync("/api/channels", account, ct);
        var created = await response.Content.ReadFromJsonAsync<ChannelAccount>(ct);

        Assert.Equal(AuthStatus.Connected, created!.AuthStatus);
        Assert.Null(created.AccessToken); // Still not exposed
    }

    [Fact]
    public async ValueTask UpdateToken_SetsConnected()
    {
        var ct = TestContext.Current.CancellationToken;
        var account = new ChannelAccount
        {
            AccountName = "token_update_test",
            ExternalAccountId = "11111111"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/channels", account, ct);
        var created = await createResponse.Content.ReadFromJsonAsync<ChannelAccount>(ct);

        var tokenRequest = new { AccessToken = "new_token_abc", TokenExpiry = DateTime.UtcNow.AddDays(60) };
        var updateResponse = await _client.PutAsJsonAsync($"/api/channels/{created!.Id}/token", tokenRequest, ct);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    }

    [Fact]
    public async ValueTask GetChannels_NeverExposesTokens()
    {
        var ct = TestContext.Current.CancellationToken;
        var account = new ChannelAccount
        {
            AccountName = "secret_token_test",
            ExternalAccountId = "22222222",
            AccessToken = "super_secret_token"
        };

        await _client.PostAsJsonAsync("/api/channels", account, ct);

        var response = await _client.GetAsync("/api/channels", ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        Assert.DoesNotContain("super_secret_token", json);
    }
}
