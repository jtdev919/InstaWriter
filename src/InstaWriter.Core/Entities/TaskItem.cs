using System.Text.Json.Serialization;

namespace InstaWriter.Core.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string Owner { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskItemStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled,
    Overdue
}
