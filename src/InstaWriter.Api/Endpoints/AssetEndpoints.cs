using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Api.Endpoints;

public static class AssetEndpoints
{
    public static RouteGroupBuilder MapAssetEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/assets").WithTags("Assets");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var assets = await db.Assets.OrderByDescending(a => a.CreatedAt).ToListAsync();
            return Results.Ok(assets);
        }).WithName("GetAssets");

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var asset = await db.Assets.FindAsync(id);
            return asset is not null ? Results.Ok(asset) : Results.NotFound();
        }).WithName("GetAssetById");

        group.MapPost("/upload", async (
            IFormFile file,
            [FromServices] IBlobStorageService blobStorage,
            [FromServices] AppDbContext db,
            [FromQuery] string? owner,
            [FromQuery] string? tags,
            [FromQuery] string? pillarName,
            [FromQuery] Guid? contentIdeaId,
            [FromQuery] Guid? contentDraftId) =>
        {
            await using var stream = file.OpenReadStream();
            var result = await blobStorage.UploadAsync(file.FileName, file.ContentType, stream);

            var asset = new Asset
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = result.FileSizeBytes,
                BlobUri = result.Uri,
                Owner = owner,
                Tags = tags,
                PillarName = pillarName,
                ContentIdeaId = contentIdeaId,
                ContentDraftId = contentDraftId,
                Status = AssetStatus.Uploaded,
                CreatedAt = DateTime.UtcNow
            };

            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            return Results.Created($"/api/assets/{asset.Id}", asset);
        })
        .DisableAntiforgery()
        .WithName("UploadAsset");

        group.MapPut("/{id:guid}", async (Guid id, Asset updated, AppDbContext db) =>
        {
            var asset = await db.Assets.FindAsync(id);
            if (asset is null) return Results.NotFound();

            asset.Owner = updated.Owner;
            asset.Tags = updated.Tags;
            asset.PillarName = updated.PillarName;
            asset.AssetType = updated.AssetType;
            asset.Status = updated.Status;
            asset.ContentIdeaId = updated.ContentIdeaId;
            asset.ContentDraftId = updated.ContentDraftId;

            await db.SaveChangesAsync();
            return Results.Ok(asset);
        }).WithName("UpdateAsset");

        group.MapGet("/{id:guid}/download", async (Guid id, [FromServices] AppDbContext db, [FromServices] IBlobStorageService blobStorage) =>
        {
            var asset = await db.Assets.FindAsync(id);
            if (asset is null) return Results.NotFound();

            var blobName = ExtractBlobName(asset.BlobUri);
            if (blobName is null) return Results.NotFound();

            var stream = await blobStorage.DownloadAsync(blobName);
            if (stream is null) return Results.NotFound();

            return Results.File(stream, asset.ContentType, asset.FileName);
        }).WithName("DownloadAsset");

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] AppDbContext db, [FromServices] IBlobStorageService blobStorage) =>
        {
            var asset = await db.Assets.FindAsync(id);
            if (asset is null) return Results.NotFound();

            var blobName = ExtractBlobName(asset.BlobUri);
            if (blobName is not null)
                await blobStorage.DeleteAsync(blobName);

            db.Assets.Remove(asset);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteAsset");

        return group;
    }

    private static string? ExtractBlobName(string? blobUri)
    {
        if (string.IsNullOrEmpty(blobUri)) return null;

        // Azure Blob URIs: https://<account>.blob.core.windows.net/<container>/<blobName>
        // The blob name is everything after the container segment in the path.
        if (Uri.TryCreate(blobUri, UriKind.Absolute, out var uri)
            && uri.Host.Contains(".blob.core.windows.net"))
        {
            var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
            return segments.Length > 1 ? segments[1] : segments[0];
        }

        // For non-Azure URIs (e.g. fake/test), the full path minus leading slash is the blob name.
        if (Uri.TryCreate(blobUri, UriKind.Absolute, out uri))
        {
            return uri.AbsolutePath.TrimStart('/');
        }

        return blobUri;
    }
}
