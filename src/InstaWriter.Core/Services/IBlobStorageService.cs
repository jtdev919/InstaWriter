namespace InstaWriter.Core.Services;

public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken ct = default);
    Task<Stream?> DownloadAsync(string blobName, CancellationToken ct = default);
    Task DeleteAsync(string blobName, CancellationToken ct = default);
    Task<string> GetPublicUrlAsync(string blobName, CancellationToken ct = default);
}

public record BlobUploadResult(string BlobName, string Uri, long FileSizeBytes);
