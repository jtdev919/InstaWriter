using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using Xunit;

namespace InstaWriter.Api.Tests;

public class ApprovalTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async ValueTask GetApprovals_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/approvals", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async ValueTask CreateApproval_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = await CreateTestDraft(ct);

        var approval = new Approval
        {
            ContentDraftId = draft.Id,
            Approver = "reviewer@example.com",
            Decision = ApprovalDecision.Approved,
            Comments = "Looks good, on-brand"
        };

        var response = await _client.PostAsJsonAsync("/api/approvals", approval, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Approval>(ct);
        Assert.NotNull(created);
        Assert.Equal("reviewer@example.com", created.Approver);
        Assert.Equal(ApprovalDecision.Approved, created.Decision);
    }

    [Fact]
    public async ValueTask CreateApproval_InvalidDraft_ReturnsBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var approval = new Approval
        {
            ContentDraftId = Guid.NewGuid(),
            Approver = "reviewer@example.com",
            Decision = ApprovalDecision.Approved
        };

        var response = await _client.PostAsJsonAsync("/api/approvals", approval, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async ValueTask CreateThenGet_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = await CreateTestDraft(ct);
        var created = await CreateTestApproval(draft.Id, ct);

        var response = await _client.GetAsync($"/api/approvals/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetched = await response.Content.ReadFromJsonAsync<Approval>(ct);
        Assert.Equal(created.Approver, fetched!.Approver);
    }

    [Fact]
    public async ValueTask GetById_NotFound_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync($"/api/approvals/{Guid.NewGuid()}", ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async ValueTask GetByDraft_ReturnsMatchingApprovals()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = await CreateTestDraft(ct);
        await CreateTestApproval(draft.Id, ct);
        await CreateTestApproval(draft.Id, ct);

        var response = await _client.GetAsync($"/api/approvals/by-draft/{draft.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var approvals = await response.Content.ReadFromJsonAsync<List<Approval>>(ct);
        Assert.NotNull(approvals);
        Assert.True(approvals.Count >= 2);
    }

    [Fact]
    public async ValueTask DeleteApproval_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = await CreateTestDraft(ct);
        var created = await CreateTestApproval(draft.Id, ct);

        var deleteResponse = await _client.DeleteAsync($"/api/approvals/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/approvals/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<ContentDraft> CreateTestDraft(CancellationToken ct)
    {
        var idea = new ContentIdea { Title = "Test Idea", Summary = "Summary", RiskLevel = ContentRiskLevel.Low };
        var ideaResponse = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResponse.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft { ContentIdeaId = createdIdea.Id, Caption = "Test caption" };
        var draftResponse = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        return (await draftResponse.Content.ReadFromJsonAsync<ContentDraft>(ct))!;
    }

    private async Task<Approval> CreateTestApproval(Guid draftId, CancellationToken ct)
    {
        var approval = new Approval
        {
            ContentDraftId = draftId,
            Approver = "reviewer@example.com",
            Decision = ApprovalDecision.Approved,
            Comments = "Approved"
        };
        var response = await _client.PostAsJsonAsync("/api/approvals", approval, ct);
        return (await response.Content.ReadFromJsonAsync<Approval>(ct))!;
    }
}
