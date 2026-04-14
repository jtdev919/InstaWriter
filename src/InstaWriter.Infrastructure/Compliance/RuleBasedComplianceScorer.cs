using InstaWriter.Core.Services;

namespace InstaWriter.Infrastructure.Compliance;

public class RuleBasedComplianceScorer : IComplianceScorer
{
    // Blocked phrases — any match forces manual review
    private static readonly string[] BlockedPhrases =
    [
        "cure",
        "treat",
        "diagnose",
        "fix your hormones",
        "this supplement will solve",
        "guaranteed results",
        "clinically proven to",
        "reverses disease",
        "heals your"
    ];

    // High-risk topic keywords — presence increases risk score
    private static readonly string[] HighRiskKeywords =
    [
        "biomarker",
        "hormone",
        "testosterone",
        "estrogen",
        "cortisol",
        "thyroid",
        "supplement",
        "diagnosis",
        "symptom",
        "blood test",
        "lab results",
        "medical",
        "prescription",
        "dosage",
        "deficiency"
    ];

    // Medium-risk topic keywords
    private static readonly string[] MediumRiskKeywords =
    [
        "recovery",
        "inflammation",
        "gut health",
        "immune",
        "detox",
        "metabolism",
        "fasting",
        "before and after",
        "transformation",
        "results"
    ];

    public ComplianceResult ScoreContent(string caption, string? script = null)
    {
        var text = string.IsNullOrEmpty(script) ? caption : $"{caption} {script}";
        var lowerText = text.ToLowerInvariant();

        var flags = new List<string>();

        // Check blocked phrases
        foreach (var phrase in BlockedPhrases)
        {
            if (lowerText.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                flags.Add($"Blocked phrase: \"{phrase}\"");
        }

        // Check high-risk keywords
        var highRiskHits = 0;
        foreach (var keyword in HighRiskKeywords)
        {
            if (lowerText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                highRiskHits++;
                flags.Add($"High-risk topic: \"{keyword}\"");
            }
        }

        // Check medium-risk keywords
        var mediumRiskHits = 0;
        foreach (var keyword in MediumRiskKeywords)
        {
            if (lowerText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                mediumRiskHits++;
                flags.Add($"Medium-risk topic: \"{keyword}\"");
            }
        }

        // Determine risk level and score
        var hasBlockedPhrases = flags.Any(f => f.StartsWith("Blocked phrase"));

        string riskLevel;
        double score;

        if (hasBlockedPhrases)
        {
            riskLevel = "High";
            score = 0.1; // Very low compliance (high risk)
        }
        else if (highRiskHits >= 2)
        {
            riskLevel = "High";
            score = 0.3;
        }
        else if (highRiskHits == 1)
        {
            riskLevel = "Medium";
            score = 0.5;
        }
        else if (mediumRiskHits >= 2)
        {
            riskLevel = "Medium";
            score = 0.6;
        }
        else if (mediumRiskHits == 1)
        {
            riskLevel = "Low";
            score = 0.8;
        }
        else
        {
            riskLevel = "Low";
            score = 1.0;
        }

        string? suggestedRewrite = null;
        if (hasBlockedPhrases)
            suggestedRewrite = "Remove or rephrase blocked medical claims. Use language like 'may support' instead of 'treats/cures'. Add 'Consult your physician' disclaimer.";

        return new ComplianceResult(score, riskLevel, flags.ToArray(), suggestedRewrite);
    }
}
