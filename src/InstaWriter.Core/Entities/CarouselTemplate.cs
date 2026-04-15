namespace InstaWriter.Core.Entities;

public class CarouselTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public int PageCount { get; set; } = 8;
    public string SlideLayoutsCsv { get; set; } = "title,content,content,content,content,content,cta-bridge,cta";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
