namespace InstaWriter.Core.Entities;

public class CalendarEvent
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public string? ExternalCalendarId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? ReminderProfile { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TaskItem? TaskItem { get; set; }
}
