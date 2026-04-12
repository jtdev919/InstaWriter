using System.Net;
using Xunit;

namespace InstaWriter.Api.Tests;

public class HealthCheckTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask HealthCheck_ReturnsExpectedShape()
    {
        var response = await _client.GetAsync("/api/health", TestContext.Current.CancellationToken);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"status\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("healthy", json, StringComparison.OrdinalIgnoreCase);
    }
}
