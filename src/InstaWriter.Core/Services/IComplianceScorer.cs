namespace InstaWriter.Core.Services;

public interface IComplianceScorer
{
    ComplianceResult ScoreContent(string caption, string? script = null);
}
