using System.Collections.Concurrent;
using InstaWriter.Core.Services;

namespace InstaWriter.Api.Tests.Fakes;

public class FakeBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, (byte[] Data, string ContentType)> _blobs = new();

    public async Task<BlobUploadResult> UploadAsync(string fileName, string contentType, Stream content, CancellationToken ct = default)
    {
        var blobName = $"{Guid.NewGuid()}/{fileName}";
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        _blobs[blobName] = (ms.ToArray(), contentType);
        return new BlobUploadResult(blobName, $"fake://assets/{blobName}", ms.Length);
    }

    public Task<Stream?> DownloadAsync(string blobName, CancellationToken ct = default)
    {
        if (_blobs.TryGetValue(blobName, out var blob))
            return Task.FromResult<Stream?>(new MemoryStream(blob.Data));
        return Task.FromResult<Stream?>(null);
    }

    public Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        _blobs.TryRemove(blobName, out _);
        return Task.CompletedTask;
    }

    public Task<string> GetPublicUrlAsync(string blobName, CancellationToken ct = default)
    {
        return Task.FromResult($"fake://assets/{blobName}");
    }
}
