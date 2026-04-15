using InstaWriter.Core.Entities;
using InstaWriter.Core.Services;

namespace InstaWriter.Infrastructure.Carousel;

/// <summary>
/// Converts a ContentBrief into a structured 8-slide carousel payload.
/// </summary>
public static class CarouselCompositionService
{
    public static CarouselRenderRequest ComposeFromBrief(ContentBrief brief, string? author = null)
    {
        var category = brief.ContentIdea?.PillarName?.ToUpperInvariant() ?? "HEALTH & PERFORMANCE";

        var slides = new List<SlideData>
        {
            // Slide 1: Title/Hook
            new("title",
                Headline: !string.IsNullOrEmpty(brief.HookDirection) ? brief.HookDirection : brief.KeyMessage,
                Subtext: brief.Objective,
                Category: $"A {category} POST",
                SlideNumber: 1),

            // Slide 2: Problem/Context
            new("content",
                Headline: "The Problem",
                Body: brief.Objective,
                Category: "THE PROBLEM",
                SlideNumber: 2),

            // Slide 3: Key Insight
            new("content",
                Headline: "Here's What Matters",
                Body: brief.KeyMessage,
                Category: "KEY INSIGHT",
                SlideNumber: 3),

            // Slide 4: Framework/Approach
            new("content",
                Headline: "The Approach",
                Body: $"For {brief.Audience}, this changes everything.",
                Highlight: brief.KeyMessage,
                Category: "THE APPROACH",
                SlideNumber: 4),

            // Slide 5: Evidence/Detail
            new("content",
                Headline: "Why It Works",
                Body: brief.Objective,
                Category: "THE EVIDENCE",
                SlideNumber: 5),

            // Slide 6: Application
            new("content",
                Headline: "How To Apply This",
                Body: $"Start with one small step. {brief.KeyMessage}",
                Category: "TAKE ACTION",
                SlideNumber: 6),

            // Slide 7: CTA Bridge
            new("cta-bridge",
                Headline: "Ready to take action?",
                Body: brief.CTA,
                SlideNumber: 7),

            // Slide 8: Final CTA
            new("cta",
                Headline: brief.CTA.Length > 0 ? brief.CTA : "Follow for more",
                CTA: "Get Started",
                Subtext: "Download free — link in bio",
                SlideNumber: 8),
        };

        return new CarouselRenderRequest("educational", slides, author ?? "HealthCoachApp");
    }
}
