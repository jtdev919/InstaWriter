using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace InstaWriter.Api.Auth;

public class ApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration config)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    private const string HeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = config["Auth:ApiKey"];

        // No API key configured — allow all requests (dev/test mode)
        if (string.IsNullOrEmpty(configuredKey))
            return Task.FromResult(AuthenticateResult.Success(CreateTicket("dev-user")));

        if (!Request.Headers.TryGetValue(HeaderName, out var apiKeyHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!string.Equals(apiKeyHeader, configuredKey, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        return Task.FromResult(AuthenticateResult.Success(CreateTicket("api-user")));
    }

    private AuthenticationTicket CreateTicket(string username)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Admin"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, SchemeName);
    }
}
