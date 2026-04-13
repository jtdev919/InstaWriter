using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Storage.Blobs;
using InstaWriter.Api.Endpoints;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.AI;
using InstaWriter.Infrastructure.Data;
using InstaWriter.Infrastructure.Instagram;
using InstaWriter.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IInstagramPublisher, InstagramPublisher>();

var azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var azureOpenAIKey = builder.Configuration["AzureOpenAI:ApiKey"];
var azureOpenAIDeployment = builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";

var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage");
if (!string.IsNullOrEmpty(blobConnectionString))
{
    var containerName = builder.Configuration["BlobStorage:ContainerName"] ?? "assets";
    builder.Services.AddSingleton(_ => new BlobContainerClient(blobConnectionString, containerName));
    builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
}

if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIKey))
{
    builder.Services.AddSingleton(new AzureOpenAIClient(
        new Uri(azureOpenAIEndpoint),
        new ApiKeyCredential(azureOpenAIKey)));

    builder.Services.AddSingleton<IContentGenerator>(sp =>
        new AzureOpenAIContentGenerator(
            sp.GetRequiredService<AzureOpenAIClient>(),
            azureOpenAIDeployment,
            sp.GetRequiredService<ILogger<AzureOpenAIContentGenerator>>()));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "healthy",
    Timestamp = DateTimeOffset.UtcNow,
    Version = "0.1.0"
}))
.WithName("HealthCheck")
.WithTags("System");

app.MapContentIdeaEndpoints();
app.MapContentDraftEndpoints();
app.MapPublishJobEndpoints();
app.MapTaskItemEndpoints();
app.MapChannelAccountEndpoints();
app.MapContentGenerationEndpoints();
app.MapAssetEndpoints();
app.MapBrandProfileEndpoints();

app.Run();

public partial class Program { }
