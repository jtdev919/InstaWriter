using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using InstaWriter.Infrastructure.Instagram;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InstaWriter.Api.Tests;

public class TokenRefreshTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask RefreshExpiringTokens_RefreshesTokenExpiringSoon()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create a channel with a token expiring in 3 days (within 7-day threshold)
        var created = await CreateChannelWithToken("refresh-test", "old_token", DateTime.UtcNow.AddDays(3), ct);

        // Run the refresh logic
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new TokenRefreshBackgroundService(scopeFactory, NullLogger<TokenRefreshBackgroundService>.Instance);
        await bgService.RefreshExpiringTokensAsync(ct);

        // Verify the token was updated
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.ChannelAccounts.FindAsync(created.Id);
        Assert.NotNull(updated);
        Assert.Equal("refreshed_token_abc", updated.AccessToken);
        Assert.Equal(AuthStatus.Connected, updated.AuthStatus);
        Assert.True(updated.TokenExpiry > DateTime.UtcNow.AddDays(30));
    }

    [Fact]
    public async ValueTask RefreshExpiringTokens_SkipsTokensNotExpiringSoon()
    {
        var ct = TestContext.Current.CancellationToken;

        // Token expires in 30 days — outside the 7-day threshold
        var created = await CreateChannelWithToken("no-refresh-test", "still_good_token", DateTime.UtcNow.AddDays(30), ct);

        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new TokenRefreshBackgroundService(scopeFactory, NullLogger<TokenRefreshBackgroundService>.Instance);
        await bgService.RefreshExpiringTokensAsync(ct);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unchanged = await db.ChannelAccounts.FindAsync(created.Id);
        Assert.NotNull(unchanged);
        Assert.Equal("still_good_token", unchanged.AccessToken);
    }

    [Fact]
    public async ValueTask RefreshExpiringTokens_PicksUpAlreadyExpiredTokens()
    {
        var ct = TestContext.Current.CancellationToken;

        // Token already expired — should still be picked up and refreshed
        var created = await CreateChannelWithToken("expired-test", "expired_token", DateTime.UtcNow.AddDays(-1), ct);

        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new TokenRefreshBackgroundService(scopeFactory, NullLogger<TokenRefreshBackgroundService>.Instance);
        await bgService.RefreshExpiringTokensAsync(ct);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var refreshed = await db.ChannelAccounts.FindAsync(created.Id);
        Assert.NotNull(refreshed);
        Assert.Equal("refreshed_token_abc", refreshed.AccessToken);
        Assert.Equal(AuthStatus.Connected, refreshed.AuthStatus);
    }

    [Fact]
    public async ValueTask RefreshExpiringTokens_SkipsInactiveAccounts()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create an inactive account with an expiring token
        var created = await CreateChannelWithToken("inactive-test", "inactive_token", DateTime.UtcNow.AddDays(2), ct);

        // Mark as inactive
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbAccount = await db.ChannelAccounts.FindAsync(created.Id);
            dbAccount!.IsActive = false;
            await db.SaveChangesAsync(ct);
        }

        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var bgService = new TokenRefreshBackgroundService(scopeFactory, NullLogger<TokenRefreshBackgroundService>.Instance);
        await bgService.RefreshExpiringTokensAsync(ct);

        // Token should NOT be refreshed
        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unchanged = await verifyDb.ChannelAccounts.FindAsync(created.Id);
        Assert.Equal("inactive_token", unchanged!.AccessToken);
    }

    private async Task<ChannelAccount> CreateChannelWithToken(string name, string token, DateTime expiry, CancellationToken ct)
    {
        // Create via API
        var account = new ChannelAccount
        {
            AccountName = name,
            ExternalAccountId = $"ig_{Guid.NewGuid():N}",
            PlatformType = PlatformType.Instagram
        };

        var response = await _client.PostAsJsonAsync("/api/channels", account, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ChannelAccount>(ct))!;

        // Set token directly in DB (API strips it from responses)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbAccount = await db.ChannelAccounts.FindAsync(created.Id);
        dbAccount!.AccessToken = token;
        dbAccount.TokenExpiry = expiry;
        dbAccount.AuthStatus = AuthStatus.Connected;
        await db.SaveChangesAsync(ct);

        return dbAccount;
    }
}
