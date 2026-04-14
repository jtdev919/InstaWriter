using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;
using InstaWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InstaWriter.Infrastructure.Analytics;

public class PerformanceAnalyticsService(AppDbContext db, ILogger<PerformanceAnalyticsService> logger) : IPerformanceAnalyticsService
{
    // Engagement score weights
    private const double LikeWeight = 1.0;
    private const double CommentWeight = 3.0;
    private const double ShareWeight = 5.0;
    private const double SaveWeight = 4.0;
    private const double ProfileVisitWeight = 2.0;
    private const double FollowWeight = 10.0;

    public async Task<PostScore> ScorePostAsync(Guid publishJobId, CancellationToken ct = default)
    {
        var job = await db.PublishJobs
            .Include(j => j.ContentDraft)
                .ThenInclude(d => d!.ContentIdea)
            .Include(j => j.ContentDraft)
                .ThenInclude(d => d!.ContentBrief)
            .FirstOrDefaultAsync(j => j.Id == publishJobId, ct)
            ?? throw new ArgumentException($"PublishJob {publishJobId} not found.");

        // Get the latest insight snapshot
        var snapshot = await db.InsightSnapshots
            .Where(s => s.PublishJobId == publishJobId)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(ct);

        if (snapshot is null)
            return new PostScore(publishJobId, job.ContentDraftId, job.ContentDraft?.ContentIdea?.PillarName,
                job.ContentDraft?.ContentBrief?.TargetFormat.ToString(), 0, 0, 0, 0);

        var totalEngagements = snapshot.Likes + snapshot.Comments + snapshot.Shares + snapshot.Saves;
        var engagementScore = CalculateEngagementScore(snapshot);
        var engagementRate = snapshot.Reach > 0 ? (double)totalEngagements / snapshot.Reach : 0;

        return new PostScore(
            publishJobId,
            job.ContentDraftId,
            job.ContentDraft?.ContentIdea?.PillarName,
            job.ContentDraft?.ContentBrief?.TargetFormat.ToString(),
            engagementScore,
            snapshot.Reach,
            totalEngagements,
            engagementRate);
    }

    public async Task<List<PostScore>> GetTopPostsAsync(int count = 10, CancellationToken ct = default)
    {
        var publishedJobs = await db.PublishJobs
            .Include(j => j.ContentDraft)
                .ThenInclude(d => d!.ContentIdea)
            .Include(j => j.ContentDraft)
                .ThenInclude(d => d!.ContentBrief)
            .Where(j => j.Status == PublishJobStatus.Published)
            .ToListAsync(ct);

        var scores = new List<PostScore>();
        foreach (var job in publishedJobs)
        {
            var snapshot = await db.InsightSnapshots
                .Where(s => s.PublishJobId == job.Id)
                .OrderByDescending(s => s.SnapshotDate)
                .FirstOrDefaultAsync(ct);

            if (snapshot is null) continue;

            var totalEngagements = snapshot.Likes + snapshot.Comments + snapshot.Shares + snapshot.Saves;
            var engagementScore = CalculateEngagementScore(snapshot);
            var engagementRate = snapshot.Reach > 0 ? (double)totalEngagements / snapshot.Reach : 0;

            scores.Add(new PostScore(
                job.Id, job.ContentDraftId,
                job.ContentDraft?.ContentIdea?.PillarName,
                job.ContentDraft?.ContentBrief?.TargetFormat.ToString(),
                engagementScore, snapshot.Reach, totalEngagements, engagementRate));
        }

        return scores.OrderByDescending(s => s.EngagementScore).Take(count).ToList();
    }

    public async Task<List<PerformanceCluster>> GetPerformanceClustersAsync(CancellationToken ct = default)
    {
        var topPosts = await GetTopPostsAsync(100, ct);
        var clusters = new List<PerformanceCluster>();

        // Cluster by format
        foreach (var group in topPosts.Where(p => p.TargetFormat != null).GroupBy(p => p.TargetFormat!))
        {
            clusters.Add(new PerformanceCluster(
                group.Key, "Format", group.Count(),
                group.Average(p => p.EngagementScore),
                group.Average(p => p.Reach),
                group.Average(p => p.EngagementRate)));
        }

        // Cluster by pillar
        foreach (var group in topPosts.Where(p => p.PillarName != null).GroupBy(p => p.PillarName!))
        {
            clusters.Add(new PerformanceCluster(
                group.Key, "Pillar", group.Count(),
                group.Average(p => p.EngagementScore),
                group.Average(p => p.Reach),
                group.Average(p => p.EngagementRate)));
        }

        return clusters.OrderByDescending(c => c.AvgEngagementScore).ToList();
    }

    public async Task<List<CTAInsight>> GetCTAInsightsAsync(CancellationToken ct = default)
    {
        // Analyze CTA patterns from published drafts
        var publishedDrafts = await db.PublishJobs
            .Include(j => j.ContentDraft)
            .Where(j => j.Status == PublishJobStatus.Published && j.ContentDraft != null)
            .ToListAsync(ct);

        var ctaGroups = new Dictionary<string, List<InsightSnapshot>>();

        foreach (var job in publishedDrafts)
        {
            var caption = job.ContentDraft!.Caption.ToLowerInvariant();
            var ctaPattern = ExtractCTAPattern(caption);

            var snapshot = await db.InsightSnapshots
                .Where(s => s.PublishJobId == job.Id)
                .OrderByDescending(s => s.SnapshotDate)
                .FirstOrDefaultAsync(ct);

            if (snapshot is null) continue;

            if (!ctaGroups.ContainsKey(ctaPattern))
                ctaGroups[ctaPattern] = [];
            ctaGroups[ctaPattern].Add(snapshot);
        }

        return ctaGroups
            .Where(g => g.Value.Count >= 1)
            .Select(g => new CTAInsight(
                g.Key,
                g.Value.Count,
                g.Value.Average(s => s.Reach > 0 ? (double)(s.Likes + s.Comments + s.Shares + s.Saves) / s.Reach : 0),
                g.Value.Average(s => s.Saves),
                g.Value.Average(s => s.Shares)))
            .OrderByDescending(c => c.AvgEngagementRate)
            .ToList();
    }

    public async Task<List<PillarPerformance>> GetPillarPerformanceAsync(CancellationToken ct = default)
    {
        var topPosts = await GetTopPostsAsync(100, ct);
        var pillars = await db.ContentPillars.ToListAsync(ct);

        var performance = new List<PillarPerformance>();

        foreach (var pillar in pillars)
        {
            var pillarPosts = topPosts.Where(p => p.PillarName == pillar.Name).ToList();
            var avgScore = pillarPosts.Count > 0 ? pillarPosts.Average(p => p.EngagementScore) : 0;

            // Recommended weight: proportional to avg engagement score relative to total
            var totalAvgScore = topPosts.Count > 0 ? topPosts.Average(p => p.EngagementScore) : 1;
            var recommendedWeight = totalAvgScore > 0 ? (avgScore / totalAvgScore) * pillar.PriorityWeight : pillar.PriorityWeight;

            performance.Add(new PillarPerformance(
                pillar.Name,
                pillarPosts.Count,
                avgScore,
                pillar.PriorityWeight,
                Math.Round(Math.Max(0.5, recommendedWeight), 2)));
        }

        return performance.OrderByDescending(p => p.AvgEngagementScore).ToList();
    }

    public async Task UpdatePillarWeightsAsync(CancellationToken ct = default)
    {
        var performance = await GetPillarPerformanceAsync(ct);

        foreach (var p in performance.Where(p => p.PostCount >= 3))
        {
            var pillar = await db.ContentPillars.FirstOrDefaultAsync(c => c.Name == p.PillarName, ct);
            if (pillar is null) continue;

            var oldWeight = pillar.PriorityWeight;
            // Blend: 70% current weight + 30% recommended weight (gradual adjustment)
            pillar.PriorityWeight = Math.Round(oldWeight * 0.7 + p.RecommendedWeight * 0.3, 2);

            logger.LogInformation("Updated pillar '{Pillar}' weight: {Old} -> {New}",
                pillar.Name, oldWeight, pillar.PriorityWeight);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<List<PostRecommendation>> GetRecommendationsAsync(int count = 5, CancellationToken ct = default)
    {
        var clusters = await GetPerformanceClustersAsync(ct);
        var pillarPerf = await GetPillarPerformanceAsync(ct);
        var ctaInsights = await GetCTAInsightsAsync(ct);

        var recommendations = new List<PostRecommendation>();

        // Recommend based on best-performing pillars that haven't been posted to recently
        var recentIdeas = await db.ContentIdeas
            .Where(i => i.CreatedAt > DateTime.UtcNow.AddDays(-14))
            .Select(i => i.PillarName)
            .ToListAsync(ct);

        var pillarFrequency = recentIdeas
            .Where(p => p != null)
            .GroupBy(p => p!)
            .ToDictionary(g => g.Key, g => g.Count());

        // High-performing pillars that are underrepresented recently
        foreach (var pillar in pillarPerf.Where(p => p.PostCount > 0).OrderByDescending(p => p.AvgEngagementScore))
        {
            var recentCount = pillarFrequency.GetValueOrDefault(pillar.PillarName, 0);
            var isUnderrepresented = recentCount < 2;

            if (isUnderrepresented || pillar.AvgEngagementScore > 0)
            {
                // Find best format for this pillar
                var bestFormat = clusters
                    .Where(c => c.GroupType == "Format")
                    .OrderByDescending(c => c.AvgEngagementScore)
                    .FirstOrDefault();

                var formatSuggestion = bestFormat?.GroupKey ?? "StaticImage";

                var confidence = pillar.PostCount >= 5 ? 0.9 :
                                 pillar.PostCount >= 3 ? 0.7 :
                                 pillar.PostCount >= 1 ? 0.5 : 0.3;

                var rationale = isUnderrepresented
                    ? $"'{pillar.PillarName}' performs well (avg score: {pillar.AvgEngagementScore:F1}) but has only {recentCount} posts in the last 14 days."
                    : $"'{pillar.PillarName}' is a top performer with avg engagement score {pillar.AvgEngagementScore:F1}.";

                recommendations.Add(new PostRecommendation(
                    pillar.PillarName, formatSuggestion, rationale, confidence));
            }

            if (recommendations.Count >= count) break;
        }

        // If we need more recommendations, suggest based on best formats
        if (recommendations.Count < count)
        {
            var pillars = await db.ContentPillars
                .OrderByDescending(p => p.PriorityWeight)
                .ToListAsync(ct);

            foreach (var pillar in pillars)
            {
                if (recommendations.Any(r => r.PillarName == pillar.Name)) continue;

                var bestFormat = clusters
                    .Where(c => c.GroupType == "Format")
                    .OrderByDescending(c => c.AvgEngagementScore)
                    .FirstOrDefault();

                recommendations.Add(new PostRecommendation(
                    pillar.Name,
                    bestFormat?.GroupKey ?? "StaticImage",
                    $"'{pillar.Name}' has high priority weight ({pillar.PriorityWeight}) but no recent performance data — worth testing.",
                    0.3));

                if (recommendations.Count >= count) break;
            }
        }

        return recommendations;
    }

    private static double CalculateEngagementScore(InsightSnapshot snapshot)
    {
        return snapshot.Likes * LikeWeight
             + snapshot.Comments * CommentWeight
             + snapshot.Shares * ShareWeight
             + snapshot.Saves * SaveWeight
             + snapshot.ProfileVisits * ProfileVisitWeight
             + snapshot.FollowsAttributed * FollowWeight;
    }

    private static string ExtractCTAPattern(string caption)
    {
        if (caption.Contains("save this")) return "Save this";
        if (caption.Contains("link in bio")) return "Link in bio";
        if (caption.Contains("share with")) return "Share with someone";
        if (caption.Contains("comment below") || caption.Contains("drop a")) return "Comment prompt";
        if (caption.Contains("follow for") || caption.Contains("follow us")) return "Follow CTA";
        if (caption.Contains("tap") || caption.Contains("click")) return "Tap/Click CTA";
        if (caption.Contains("learn more")) return "Learn more";
        if (caption.Contains("try this") || caption.Contains("try it")) return "Try this";
        return "No explicit CTA";
    }
}
