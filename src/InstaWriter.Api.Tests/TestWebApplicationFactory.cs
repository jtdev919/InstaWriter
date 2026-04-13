using InstaWriter.Api.Tests.Fakes;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InstaWriter.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public TestWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related registrations
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.FullName?.Contains("DbContextOptions") == true
                         || d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                         || d.ServiceType == typeof(AppDbContext))
                .ToList();

            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Add SQLite in-memory for tests
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            // Replace Instagram publisher with fake
            var publisherDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IInstagramPublisher));
            if (publisherDescriptor != null) services.Remove(publisherDescriptor);
            services.AddSingleton<IInstagramPublisher>(new FakeInstagramPublisher());

            // Replace content generator with fake
            var generatorDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IContentGenerator));
            if (generatorDescriptor != null) services.Remove(generatorDescriptor);
            services.AddSingleton<IContentGenerator>(new FakeContentGenerator());

            // Replace blob storage with fake
            var blobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBlobStorageService));
            if (blobDescriptor != null) services.Remove(blobDescriptor);
            services.AddSingleton<IBlobStorageService>(new FakeBlobStorageService());

            // Replace token refresh with fake
            var tokenRefreshDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITokenRefreshService));
            if (tokenRefreshDescriptor != null) services.Remove(tokenRefreshDescriptor);
            services.AddSingleton<ITokenRefreshService>(new FakeTokenRefreshService());

            // Remove background services for tests
            var hostedServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
            foreach (var hs in hostedServices) services.Remove(hs);

            // Ensure the schema is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
