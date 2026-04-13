using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class WorkflowEventTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetWorkflowEvents_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/workflow-events", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask CreateWorkflowEvent_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var wfEvent = new WorkflowEvent
        {
            EventType = "StatusTransition",
            EntityType = "ContentIdea",
            EntityId = Guid.NewGuid(),
            PayloadJson = "{\"from\":\"IdeaCaptured\",\"to\":\"BriefReady\"}",
            CorrelationId = "corr-001"
        };

        var response = await _client.PostAsJsonAsync("/api/workflow-events", wfEvent, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<WorkflowEvent>(ct);
        Assert.NotNull(created);
        Assert.Equal("StatusTransition", created.EventType);
        Assert.Equal("ContentIdea", created.EntityType);
        Assert.Equal("corr-001", created.CorrelationId);
    }

    [Fact]
    public async ValueTask CreateThenGet_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateTestEvent(ct);

        var response = await _client.GetAsync($"/api/workflow-events/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<WorkflowEvent>(ct);
        Assert.Equal(created.EventType, fetched!.EventType);
    }

    [Fact]
    public async ValueTask GetById_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/workflow-events/{Guid.NewGuid()}", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetByEntity_ReturnsMatchingEvents()
    {
        var ct = TestContext.Current.CancellationToken;
        var entityId = Guid.NewGuid();

        var ev1 = new WorkflowEvent { EventType = "Created", EntityType = "ContentDraft", EntityId = entityId };
        var ev2 = new WorkflowEvent { EventType = "Approved", EntityType = "ContentDraft", EntityId = entityId };
        await _client.PostAsJsonAsync("/api/workflow-events", ev1, ct);
        await _client.PostAsJsonAsync("/api/workflow-events", ev2, ct);

        var response = await _client.GetAsync($"/api/workflow-events/by-entity/ContentDraft/{entityId}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<List<WorkflowEvent>>(ct);
        Assert.NotNull(events);
        Assert.True(events.Count >= 2);
    }

    [Fact]
    public async ValueTask GetByCorrelation_ReturnsMatchingEvents()
    {
        var ct = TestContext.Current.CancellationToken;
        var correlationId = $"corr-{Guid.NewGuid():N}";

        var ev1 = new WorkflowEvent { EventType = "Step1", EntityType = "Pipeline", EntityId = Guid.NewGuid(), CorrelationId = correlationId };
        var ev2 = new WorkflowEvent { EventType = "Step2", EntityType = "Pipeline", EntityId = Guid.NewGuid(), CorrelationId = correlationId };
        await _client.PostAsJsonAsync("/api/workflow-events", ev1, ct);
        await _client.PostAsJsonAsync("/api/workflow-events", ev2, ct);

        var response = await _client.GetAsync($"/api/workflow-events/by-correlation/{correlationId}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<List<WorkflowEvent>>(ct);
        Assert.NotNull(events);
        Assert.True(events.Count >= 2);
    }

    private async Task<WorkflowEvent> CreateTestEvent(CancellationToken ct)
    {
        var wfEvent = new WorkflowEvent
        {
            EventType = "StatusTransition",
            EntityType = "ContentIdea",
            EntityId = Guid.NewGuid()
        };
        var response = await _client.PostAsJsonAsync("/api/workflow-events", wfEvent, ct);
        return (await response.Content.ReadFromJsonAsync<WorkflowEvent>(ct))!;
    }
}
