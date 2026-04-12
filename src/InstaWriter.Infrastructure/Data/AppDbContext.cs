using InstaWriter.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstaWriter.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ContentIdea> ContentIdeas => Set<ContentIdea>();
    public DbSet<ContentDraft> ContentDrafts => Set<ContentDraft>();
    public DbSet<PublishJob> PublishJobs => Set<PublishJob>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
