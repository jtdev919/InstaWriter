using InstaWriter.Core.Services;

namespace InstaWriter.Api.Tests.Fakes;

public class FakeCarouselRenderer : ICarouselRenderer
{
    public Task<List<RenderedSlide>> RenderCarouselAsync(CarouselRenderRequest request, CancellationToken ct = default)
    {
        var slides = new List<RenderedSlide>();
        for (var i = 0; i < request.Slides.Count; i++)
        {
            var slide = request.Slides[i];
            // Generate a minimal valid PNG (1x1 pixel)
            var fakePng = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
            slides.Add(new RenderedSlide(i + 1, $"slide-{i + 1:D2}-{slide.Layout}.png", fakePng));
        }
        return Task.FromResult(slides);
    }
}
