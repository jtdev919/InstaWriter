using InstaWriter.Core.Services;

namespace InstaWriter.Infrastructure.Storage;

public class NullBlobStorageService : IBlobStorageService
{
    public Task<BlobUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken ct = default)
        => throw new InvalidOperationException("Blob storage is not configured. Set ConnectionStrings:BlobStorage in appsettings.");

    public Task<Stream?> DownloadAsync(string blobName, CancellationToken ct = default)
        => throw new InvalidOperationException("Blob storage is not configured.");

    public Task DeleteAsync(string blobName, CancellationToken ct = default)
        => throw new InvalidOperationException("Blob storage is not configured.");

    public Task<string> GetPublicUrlAsync(string blobName, CancellationToken ct = default)
        => throw new InvalidOperationException("Blob storage is not configured.");
}
