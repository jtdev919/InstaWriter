using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class TaskItemTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask PostTask_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = new TaskItem
        {
            Owner = "Joe",
            TaskType = "RecordReel",
            Description = "Record founder Reel for this week"
        };

        var response = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TaskItem>(ct);
        Assert.NotNull(created);
        Assert.Equal(TaskItemStatus.Pending, created.Status);
        Assert.Equal(TaskPriority.Medium, created.Priority);
    }

    [Fact]
    public async ValueTask CompleteTask_SetsCompleted()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = new TaskItem
        {
            Owner = "Joe",
            TaskType = "ApproveCaption",
            Description = "Approve Friday carousel caption"
        };

        var postResponse = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        var created = await postResponse.Content.ReadFromJsonAsync<TaskItem>(ct);

        // Must transition to InProgress before completing
        await _client.PostAsJsonAsync($"/api/tasks/{created!.Id}/transition", new { Status = "InProgress" }, ct);

        var completeResponse = await _client.PostAsync($"/api/tasks/{created.Id}/complete", null, ct);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        var completed = await completeResponse.Content.ReadFromJsonAsync<TaskItem>(ct);
        Assert.Equal(TaskItemStatus.Completed, completed!.Status);
    }

    [Fact]
    public async ValueTask GetTaskById_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = new TaskItem
        {
            Owner = "Joe",
            TaskType = "UploadMedia",
            Priority = TaskPriority.High
        };

        var postResponse = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        var created = await postResponse.Content.ReadFromJsonAsync<TaskItem>(ct);

        var getResponse = await _client.GetAsync($"/api/tasks/{created!.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<TaskItem>(ct);
        Assert.Equal("UploadMedia", fetched!.TaskType);
        Assert.Equal(TaskPriority.High, fetched.Priority);
    }

    [Fact]
    public async ValueTask DeleteTask_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var task = new TaskItem { Owner = "Joe", TaskType = "Delete me" };
        var postResponse = await _client.PostAsJsonAsync("/api/tasks", task, ct);
        var created = await postResponse.Content.ReadFromJsonAsync<TaskItem>(ct);

        var deleteResponse = await _client.DeleteAsync($"/api/tasks/{created!.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/tasks/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
