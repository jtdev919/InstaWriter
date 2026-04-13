namespace InstaWriter.Core.Entities;

public class ContentPillar
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double PriorityWeight { get; set; } = 1.0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
