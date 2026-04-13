using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class AssetTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetAssets_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/assets", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetAssets_ReturnsList()
    {
        var ct = TestContext.Current.CancellationToken;
        var assets = await _client.GetFromJsonAsync<List<Asset>>("/api/assets", ct);
        Assert.NotNull(assets);
    }

    [Fact]
    public async ValueTask UploadAsset_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("fake image data"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "test.png");

        var response = await _client.PostAsync("/api/assets/upload?owner=test-user&tags=fitness", content, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var asset = await response.Content.ReadFromJsonAsync<Asset>(ct);
        Assert.NotNull(asset);
        Assert.Equal("test.png", asset.FileName);
        Assert.Equal("image/png", asset.ContentType);
        Assert.Equal("test-user", asset.Owner);
        Assert.Equal("fitness", asset.Tags);
        Assert.NotNull(asset.BlobUri);
        Assert.True(asset.FileSizeBytes > 0);
    }

    [Fact]
    public async ValueTask UploadThenGet_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var uploaded = await UploadTestAsset(ct);

        var response = await _client.GetAsync($"/api/assets/{uploaded.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<Asset>(ct);
        Assert.Equal(uploaded.FileName, fetched!.FileName);
    }

    [Fact]
    public async ValueTask GetById_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/assets/{Guid.NewGuid()}", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask UpdateAsset_ChangesMetadata()
    {
        var ct = TestContext.Current.CancellationToken;
        var uploaded = await UploadTestAsset(ct);

        uploaded.Owner = "updated-owner";
        uploaded.Tags = "updated-tags";
        uploaded.AssetType = AssetType.Screenshot;

        var putResponse = await _client.PutAsJsonAsync($"/api/assets/{uploaded.Id}", uploaded, ct);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<Asset>(ct);
        Assert.Equal("updated-owner", updated!.Owner);
        Assert.Equal("updated-tags", updated.Tags);
        Assert.Equal(AssetType.Screenshot, updated.AssetType);
    }

    [Fact]
    public async ValueTask DownloadAsset_ReturnsFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var uploaded = await UploadTestAsset(ct);

        var response = await _client.GetAsync($"/api/assets/{uploaded.Id}/download", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async ValueTask DeleteAsset_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var uploaded = await UploadTestAsset(ct);

        var deleteResponse = await _client.DeleteAsync($"/api/assets/{uploaded.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/assets/{uploaded.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<Asset> UploadTestAsset(CancellationToken ct)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("fake image data"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "test.png");

        var response = await _client.PostAsync("/api/assets/upload", content, ct);
        return (await response.Content.ReadFromJsonAsync<Asset>(ct))!;
    }
}
