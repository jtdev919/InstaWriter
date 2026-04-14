using System.Net;
using System.Net.Http.Json;
using InstaWriter.Core.Entities;
using InstaWriter.Infrastructure.Compliance;
using Xunit;

namespace InstaWriter.Api.Tests;

public class ComplianceScoringTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // --- Rule-based scorer unit tests ---

    [Fact]
    public void CleanContent_ScoresLowRisk()
    {
        var scorer = new RuleBasedComplianceScorer();
        var result = scorer.ScoreContent("Start your morning with a walk and a good playlist");

        Assert.Equal("Low", result.RiskLevel);
        Assert.Equal(1.0, result.Score);
        Assert.Empty(result.Flags);
    }

    [Fact]
    public void BlockedPhrase_ScoresHighRisk()
    {
        var scorer = new RuleBasedComplianceScorer();
        var result = scorer.ScoreContent("This supplement will cure your fatigue");

        Assert.Equal("High", result.RiskLevel);
        Assert.True(result.Score <= 0.3);
        Assert.Contains(result.Flags, f => f.Contains("cure"));
        Assert.NotNull(result.SuggestedRewrite);
    }

    [Fact]
    public void MultipleHighRiskKeywords_ScoresHighRisk()
    {
        var scorer = new RuleBasedComplianceScorer();
        var result = scorer.ScoreContent("Check your testosterone and cortisol levels with our biomarker panel");

        Assert.Equal("High", result.RiskLevel);
        Assert.True(result.Score <= 0.3);
    }

    [Fact]
    public void SingleHighRiskKeyword_ScoresMediumRisk()
    {
        var scorer = new RuleBasedComplianceScorer();
        var result = scorer.ScoreContent("Understanding your cortisol patterns can help with stress management");

        Assert.Equal("Medium", result.RiskLevel);
        Assert.Equal(0.5, result.Score);
    }

    [Fact]
    public void MediumRiskKeywords_ScoresMediumRisk()
    {
        var scorer = new RuleBasedComplianceScorer();
        var result = scorer.ScoreContent("My gut health transformation with intermittent fasting results");

        Assert.Equal("Medium", result.RiskLevel);
    }

    [Fact]
    public void ScriptIncludedInScoring()
    {
        var scorer = new RuleBasedComplianceScorer();
        var result = scorer.ScoreContent("Check out this Reel", "This supplement will treat your hormone issues");

        Assert.Equal("High", result.RiskLevel);
        Assert.Contains(result.Flags, f => f.Contains("treat"));
        Assert.Contains(result.Flags, f => f.Contains("supplement"));
        Assert.Contains(result.Flags, f => f.Contains("hormone"));
    }

    // --- Integration tests: auto-routing via orchestration ---

    [Fact]
    public async ValueTask LowRiskDraft_AutoApproved_WhenSubmittedForReview()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create a low-risk idea + safe draft
        var idea = new ContentIdea { Title = "Morning Routine Tips", RiskLevel = ContentRiskLevel.Low };
        var ideaResp = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResp.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft
        {
            ContentIdeaId = createdIdea.Id,
            Caption = "Start your morning with gratitude and movement. Save this for later!"
        };
        var draftResp = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResp.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        // Transition to AwaitingReview — should be auto-approved
        await _client.PostAsJsonAsync($"/api/content/drafts/{createdDraft.Id}/transition", new { Status = "AwaitingReview" }, ct);

        // Fetch the draft — it should now be Approved (auto-routed)
        var fetchResp = await _client.GetAsync($"/api/content/drafts/{createdDraft.Id}", ct);
        var fetchedDraft = await fetchResp.Content.ReadFromJsonAsync<ContentDraft>(ct);
        Assert.Equal(ContentDraftStatus.Approved, fetchedDraft!.Status);
        Assert.True(fetchedDraft.ComplianceScore >= 0.8);

        // Should have an auto-approval record
        var approvals = await _client.GetFromJsonAsync<List<Approval>>($"/api/approvals/by-draft/{createdDraft.Id}", ct);
        Assert.NotNull(approvals);
        Assert.Contains(approvals, a => a.Approver == "system:auto-approve" && a.Decision == ApprovalDecision.Approved);

        // Should have auto-created a PublishJob
        var jobs = await _client.GetFromJsonAsync<List<PublishJob>>("/api/publish/jobs", ct);
        Assert.Contains(jobs!, j => j.ContentDraftId == createdDraft.Id);
    }

    [Fact]
    public async ValueTask HighRiskDraft_RequiresManualReview()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create a low-risk idea but with a risky caption
        var idea = new ContentIdea { Title = "Supplement Guide", RiskLevel = ContentRiskLevel.Low };
        var ideaResp = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResp.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft
        {
            ContentIdeaId = createdIdea.Id,
            Caption = "This supplement will cure your hormone imbalance and treat your symptoms"
        };
        var draftResp = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResp.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        // Transition to AwaitingReview — should NOT be auto-approved
        await _client.PostAsJsonAsync($"/api/content/drafts/{createdDraft.Id}/transition", new { Status = "AwaitingReview" }, ct);

        // Fetch the draft — should still be AwaitingReview
        var fetchResp = await _client.GetAsync($"/api/content/drafts/{createdDraft.Id}", ct);
        var fetchedDraft = await fetchResp.Content.ReadFromJsonAsync<ContentDraft>(ct);
        Assert.Equal(ContentDraftStatus.AwaitingReview, fetchedDraft!.Status);
        Assert.True(fetchedDraft.ComplianceScore < 0.8);

        // Should have a pending approval with compliance flags
        var approvals = await _client.GetFromJsonAsync<List<Approval>>($"/api/approvals/by-draft/{createdDraft.Id}", ct);
        Assert.NotNull(approvals);
        Assert.Contains(approvals, a => a.Decision == ApprovalDecision.Pending);
        Assert.Contains(approvals, a => a.Comments != null && a.Comments.Contains("Compliance flags"));
    }

    [Fact]
    public async ValueTask MediumRiskIdea_NeverAutoApproved()
    {
        var ct = TestContext.Current.CancellationToken;

        // Medium-risk idea with safe caption — should still require review
        var idea = new ContentIdea { Title = "Health Tips", RiskLevel = ContentRiskLevel.Medium };
        var ideaResp = await _client.PostAsJsonAsync("/api/content/ideas", idea, ct);
        var createdIdea = (await ideaResp.Content.ReadFromJsonAsync<ContentIdea>(ct))!;

        var draft = new ContentDraft
        {
            ContentIdeaId = createdIdea.Id,
            Caption = "A simple morning walk can make your whole day better"
        };
        var draftResp = await _client.PostAsJsonAsync("/api/content/drafts", draft, ct);
        var createdDraft = (await draftResp.Content.ReadFromJsonAsync<ContentDraft>(ct))!;

        await _client.PostAsJsonAsync($"/api/content/drafts/{createdDraft.Id}/transition", new { Status = "AwaitingReview" }, ct);

        var fetchResp = await _client.GetAsync($"/api/content/drafts/{createdDraft.Id}", ct);
        var fetchedDraft = await fetchResp.Content.ReadFromJsonAsync<ContentDraft>(ct);
        Assert.Equal(ContentDraftStatus.AwaitingReview, fetchedDraft!.Status);
    }
}
