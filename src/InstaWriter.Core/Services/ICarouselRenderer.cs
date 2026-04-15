namespace InstaWriter.Core.Services;

public interface ICarouselRenderer
{
    Task<List<RenderedSlide>> RenderCarouselAsync(CarouselRenderRequest request, CancellationToken ct = default);
}

public record CarouselRenderRequest(
    string TemplateType,
    List<SlideData> Slides,
    string? Author = null);

public record SlideData(
    string Layout,       // "title", "content", "cta-bridge", "cta"
    string Headline,
    string? Body = null,
    string? Subtext = null,
    string? Highlight = null,
    string? CTA = null,
    string? Category = null,
    int SlideNumber = 0);

public record RenderedSlide(
    int PageNumber,
    string FileName,
    byte[] PngData);
