using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class CalendarEventTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetCalendarEvents_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/calendar-events", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask CreateCalendarEvent_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = await CreateTestTask(ct);

        var calEvent = new CalendarEvent
        {
            TaskItemId = task.Id,
            StartDateTime = DateTime.UtcNow.AddDays(1),
            EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            ReminderProfile = "30min-before"
        };

        var response = await _client.PostAsJsonAsync("/api/calendar-events", calEvent, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CalendarEvent>(ct);
        Assert.NotNull(created);
        Assert.Equal(task.Id, created.TaskItemId);
        Assert.Equal("30min-before", created.ReminderProfile);
    }

    [Fact]
    public async ValueTask CreateCalendarEvent_InvalidTask_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var calEvent = new CalendarEvent
        {
            TaskItemId = Guid.NewGuid(),
            StartDateTime = DateTime.UtcNow,
            EndDateTime = DateTime.UtcNow.AddHours(1)
        };

        var response = await _client.PostAsJsonAsync("/api/calendar-events", calEvent, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask CreateThenGet_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestCalendarEvent(ct);

        var response = await _client.GetAsync($"/api/calendar-events/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<CalendarEvent>(ct);
        Assert.Equal(created.TaskItemId, fetched!.TaskItemId);
    }

    [Fact]
    public async ValueTask GetById_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/calendar-events/{Guid.NewGuid()}", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetByTask_ReturnsMatchingEvents()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = await CreateTestTask(ct);

        var ev1 = new CalendarEvent { TaskItemId = task.Id, StartDateTime = DateTime.UtcNow.AddDays(1), EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(1) };
        var ev2 = new CalendarEvent { TaskItemId = task.Id, StartDateTime = DateTime.UtcNow.AddDays(2), EndDateTime = DateTime.UtcNow.AddDays(2).AddHours(1) };
        await _client.PostAsJsonAsync("/api/calendar-events", ev1, ct);
        await _client.PostAsJsonAsync("/api/calendar-events", ev2, ct);

        var response = await _client.GetAsync($"/api/calendar-events/by-task/{task.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<List<CalendarEvent>>(ct);
        Assert.NotNull(events);
        Assert.True(events.Count >= 2);
    }

    [Fact]
    public async ValueTask UpdateCalendarEvent_ChangesFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestCalendarEvent(ct);

        created.ReminderProfile = "1hr-before";
        created.ExternalCalendarId = "ext-cal-123";

        var putResponse = await _client.PutAsJsonAsync($"/api/calendar-events/{created.Id}", created, ct);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<CalendarEvent>(ct);
        Assert.Equal("1hr-before", updated!.ReminderProfile);
        Assert.Equal("ext-cal-123", updated.ExternalCalendarId);
    }

    [Fact]
    public async ValueTask DeleteCalendarEvent_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestCalendarEvent(ct);

        var deleteResponse = await _client.DeleteAsync($"/api/calendar-events/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/calendar-events/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<TaskItem> CreateTestTask(CancellationToken ct)
    {
        var task = new TaskItem { TaskType = "Recording", Description = "Film founder Reel" };
        var response = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        return (await response.Content.ReadFromJsonAsync<TaskItem>(ct))!;
    }

    private async Task<CalendarEvent> CreateTestCalendarEvent(CancellationToken ct)
    {
        var task = await CreateTestTask(ct);
        var calEvent = new CalendarEvent
        {
            TaskItemId = task.Id,
            StartDateTime = DateTime.UtcNow.AddDays(1),
            EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            ReminderProfile = "30min-before"
        };
        var response = await _client.PostAsJsonAsync("/api/calendar-events", calEvent, ct);
        return (await response.Content.ReadFromJsonAsync<CalendarEvent>(ct))!;
    }
}
