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
    public DbSet<ChannelAccount> ChannelAccounts => Set<ChannelAccount>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<BrandProfile> BrandProfiles => Set<BrandProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
