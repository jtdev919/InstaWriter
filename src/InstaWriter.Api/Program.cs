using InstaWriter.Api.Endpoints;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.Run();

public partial class Program { }
