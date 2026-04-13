namespace InstaWriter.Core.Entities;

public class WorkflowEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
    public string? PayloadJson { get; set; }
    public string? CorrelationId { get; set; }
}
