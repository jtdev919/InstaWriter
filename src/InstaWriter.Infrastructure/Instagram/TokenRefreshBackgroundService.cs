using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Instagram;

public class TokenRefreshBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TokenRefreshBackgroundService> logger) : BackgroundService
{
    // Check every 6 hours
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    // Refresh tokens expiring within 7 days
    private static readonly TimeSpan RefreshThreshold = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Token refresh background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshExpiringTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during token refresh cycle");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    public async Task RefreshExpiringTokensAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var refreshService = scope.ServiceProvider.GetRequiredService<ITokenRefreshService>();

        var threshold = DateTime.UtcNow.Add(RefreshThreshold);

        var accountsToRefresh = await db.ChannelAccounts
            .Where(a => a.IsActive
                && a.AuthStatus == AuthStatus.Connected
                && a.AccessToken != null
                && a.TokenExpiry != null
                && a.TokenExpiry <= threshold)
            .ToListAsync(ct);

        if (accountsToRefresh.Count == 0)
        {
            logger.LogDebug("No tokens need refreshing");
            return;
        }

        logger.LogInformation("Found {Count} token(s) to refresh", accountsToRefresh.Count);

        foreach (var account in accountsToRefresh)
        {
            logger.LogInformation("Refreshing token for account {AccountId} ({AccountName}), expires {Expiry}",
                account.Id, account.AccountName, account.TokenExpiry);

            var result = await refreshService.RefreshLongLivedTokenAsync(account.AccessToken!, ct);

            if (result.Success)
            {
                account.AccessToken = result.NewToken;
                account.TokenExpiry = result.ExpiresAt;
                account.AuthStatus = AuthStatus.Connected;

                logger.LogInformation("Token refreshed for account {AccountId}, new expiry {Expiry}",
                    account.Id, result.ExpiresAt);
            }
            else
            {
                logger.LogWarning("Token refresh failed for account {AccountId}: {Error}",
                    account.Id, result.Error);

                // If the token has already expired, mark it
                if (account.TokenExpiry < DateTime.UtcNow)
                {
                    account.AuthStatus = AuthStatus.Expired;
                    logger.LogWarning("Token for account {AccountId} has expired, marking as Expired", account.Id);
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
