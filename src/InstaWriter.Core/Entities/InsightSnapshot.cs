namespace InstaWriter.Core.Entities;

public class InsightSnapshot
{
    public Guid Id { get; set; }
    public Guid PublishJobId { get; set; }
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    public int Reach { get; set; }
    public int Views { get; set; }
    public int Likes { get; set; }
    public int Comments { get; set; }
    public int Shares { get; set; }
    public int Saves { get; set; }
    public int ProfileVisits { get; set; }
    public int FollowsAttributed { get; set; }

    public PublishJob? PublishJob { get; set; }
}
