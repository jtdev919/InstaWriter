using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Storage.Blobs;
using InstaWriter.Api.Endpoints;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.AI;
using InstaWriter.Infrastructure.Data;
using InstaWriter.Infrastructure.Analytics;
using InstaWriter.Infrastructure.Carousel;
using InstaWriter.Infrastructure.Compliance;
using InstaWriter.Infrastructure.Instagram;
using InstaWriter.Infrastructure.Notifications;
using InstaWriter.Infrastructure.Orchestration;
using InstaWriter.Infrastructure.Storage;
using InstaWriter.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddAuthentication(ApiKeyAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(ApiKeyAuthHandler.SchemeName, null);
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
    options.AddPolicy("ProdCors", policy =>
        policy.WithOrigins("https://instawriter-api.azurewebsites.net")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IInstagramPublisher, InstagramPublisher>();
builder.Services.AddHttpClient<ITokenRefreshService, MetaTokenRefreshService>();
builder.Services.AddHttpClient<IInsightsService, InstagramInsightsService>();
builder.Services.AddSingleton<IComplianceScorer, RuleBasedComplianceScorer>();
builder.Services.AddSingleton<ICarouselRenderer, PlaywrightCarouselRenderer>();
builder.Services.AddScoped<IFallbackSubstitutionService, FallbackSubstitutionService>();
builder.Services.AddScoped<IPerformanceAnalyticsService, PerformanceAnalyticsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<INotificationChannelSender, EmailNotificationSender>();
builder.Services.AddSingleton<INotificationChannelSender, SlackNotificationSender>();
builder.Services.AddScoped<IOrchestrationService, OrchestrationService>();
builder.Services.AddHostedService<TokenRefreshBackgroundService>();
builder.Services.AddHostedService<InsightsCollectionBackgroundService>();
builder.Services.AddHostedService<DeadlineEscalationBackgroundService>();

var azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var azureOpenAIKey = builder.Configuration["AzureOpenAI:ApiKey"];
var azureOpenAIDeployment = builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";

var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage")
    ?? builder.Configuration["BlobStorage:ConnectionString"];
if (!string.IsNullOrEmpty(blobConnectionString))
{
    var containerName = builder.Configuration["BlobStorage:ContainerName"] ?? "assets";
    builder.Services.AddSingleton(_ => new BlobContainerClient(blobConnectionString, containerName));
    builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
}
else
{
    // Register a no-op blob service so DI resolves and endpoints don't fail at startup
    builder.Services.AddSingleton<IBlobStorageService>(new InstaWriter.Infrastructure.Storage.NullBlobStorageService());
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
    app.UseCors("DevCors");
    app.MapOpenApi();
}
else
{
    app.UseCors("ProdCors");
}

// Auto-migrate on startup (single-user app, safe to do in all environments)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "healthy",
    Timestamp = DateTimeOffset.UtcNow,
    Version = "0.1.0"
}))
.WithName("HealthCheck")
.WithTags("System")
.AllowAnonymous();

app.MapContentIdeaEndpoints().RequireAuthorization();
app.MapContentDraftEndpoints().RequireAuthorization();
app.MapPublishJobEndpoints().RequireAuthorization();
app.MapTaskItemEndpoints().RequireAuthorization();
app.MapChannelAccountEndpoints().RequireAuthorization();
app.MapContentGenerationEndpoints().RequireAuthorization();
app.MapAssetEndpoints().RequireAuthorization();
app.MapBrandProfileEndpoints().RequireAuthorization();
app.MapContentBriefEndpoints().RequireAuthorization();
app.MapApprovalEndpoints().RequireAuthorization();
app.MapCalendarEventEndpoints().RequireAuthorization();
app.MapWorkflowEventEndpoints().RequireAuthorization();
app.MapInsightSnapshotEndpoints().RequireAuthorization();
app.MapCampaignEndpoints().RequireAuthorization();
app.MapContentPillarEndpoints().RequireAuthorization();
app.MapAnalyticsEndpoints().RequireAuthorization();
app.MapNotificationEndpoints().RequireAuthorization();

app.Run();

public partial class Program { }
