using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstaWriter.Api.Tests;

public static class TestHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static Task<T?> ReadJsonAsync<T>(this HttpContent content, CancellationToken ct = default) =>
        content.ReadFromJsonAsync<T>(JsonOptions, ct);
}
