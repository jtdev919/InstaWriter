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
    public DbSet<ContentBrief> ContentBriefs => Set<ContentBrief>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<WorkflowEvent> WorkflowEvents => Set<WorkflowEvent>();
    public DbSet<InsightSnapshot> InsightSnapshots => Set<InsightSnapshot>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<ContentPillar> ContentPillars => Set<ContentPillar>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CarouselTemplate> CarouselTemplates => Set<CarouselTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
