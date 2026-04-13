using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class ChannelAccountEndpoints
{
    public static RouteGroupBuilder MapChannelAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/channels").WithTags("Channel Accounts");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var accounts = await db.ChannelAccounts
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            // Never expose tokens in list responses
            foreach (var a in accounts) a.AccessToken = null;
            return Results.Ok(accounts);
        }).WithName("GetChannelAccounts");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var account = await db.ChannelAccounts.FindAsync(id);
            if (account is null) return Results.NotFound();

            account.AccessToken = null;
            return Results.Ok(account);
        }).WithName("GetChannelAccountById");

        group.MapPost("/", async (ChannelAccount account, AppDbContext db) =>
        {
            account.Id = Guid.NewGuid();
            account.CreatedAt = DateTime.UtcNow;
            account.AuthStatus = string.IsNullOrWhiteSpace(account.AccessToken)
                ? AuthStatus.Pending
                : AuthStatus.Connected;

            db.ChannelAccounts.Add(account);
            await db.SaveChangesAsync();

            account.AccessToken = null;
            return Results.Created($"/api/channels/{account.Id}", account);
        }).WithName("CreateChannelAccount");

        group.MapPut("/{id:guid}/token", async (Guid id, TokenUpdateRequest request, AppDbContext db) =>
        {
            var account = await db.ChannelAccounts.FindAsync(id);
            if (account is null) return Results.NotFound();

            account.AccessToken = request.AccessToken;
            account.TokenExpiry = request.TokenExpiry;
            account.AuthStatus = AuthStatus.Connected;

            await db.SaveChangesAsync();

            return Results.Ok(new { account.Id, account.AuthStatus, account.TokenExpiry });
        }).WithName("UpdateChannelToken");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var account = await db.ChannelAccounts.FindAsync(id);
            if (account is null) return Results.NotFound();

            db.ChannelAccounts.Remove(account);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteChannelAccount");

        return group;
    }

    public record TokenUpdateRequest(string AccessToken, DateTime? TokenExpiry);
}
