using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InstaWriter.Core.Services;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Storage;

public class AzureBlobStorageService(BlobContainerClient containerClient, ILogger<AzureBlobStorageService> logger) : IBlobStorageService
{
    public async Task<BlobUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken ct = default)
    {
        var blobName = $"{Guid.NewGuid()}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        logger.LogInformation("Uploading blob: {BlobName}", blobName);

        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        var properties = await blobClient.GetPropertiesAsync(cancellationToken: ct);

        return new BlobUploadResult(blobName, blobClient.Uri.ToString(), properties.Value.ContentLength);
    }

    public async Task<Stream?> DownloadAsync(string blobName, CancellationToken ct = default)
    {
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(ct))
            return null;

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
        logger.LogInformation("Deleted blob: {BlobName}", blobName);
    }

    public Task<string> GetPublicUrlAsync(string blobName, CancellationToken ct = default)
    {
        var blobClient = containerClient.GetBlobClient(blobName);
        return Task.FromResult(blobClient.Uri.ToString());
    }
}
