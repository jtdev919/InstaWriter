namespace InstaWriter.Core.Entities;

public class BrandProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VoiceGuide { get; set; } = string.Empty;
    public string ToneGuide { get; set; } = string.Empty;
    public string CTAStyle { get; set; } = string.Empty;
    public string DisclaimerRules { get; set; } = string.Empty;
    public string DefaultHashtagSets { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
