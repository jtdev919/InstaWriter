using System.Text.RegularExpressions;
using InstaWriter.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace InstaWriter.Infrastructure.Carousel;

public partial class PlaywrightCarouselRenderer : ICarouselRenderer, IAsyncDisposable
{
    private readonly ILogger<PlaywrightCarouselRenderer> _logger;
    private readonly string _templatesPath;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public PlaywrightCarouselRenderer(ILogger<PlaywrightCarouselRenderer> logger)
    {
        _logger = logger;

        // Resolve templates path — look next to the executing assembly
        var assemblyDir = Path.GetDirectoryName(typeof(PlaywrightCarouselRenderer).Assembly.Location)!;
        _templatesPath = Path.Combine(assemblyDir, "Carousel", "Templates");

        if (!Directory.Exists(_templatesPath))
        {
            // Fallback: look relative to working directory
            _templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "Carousel", "Templates");
        }
    }

    private async Task EnsureBrowserAsync()
    {
        if (_browser != null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_browser != null) return;

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            _logger.LogInformation("Playwright browser launched for carousel rendering");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<RenderedSlide>> RenderCarouselAsync(CarouselRenderRequest request, CancellationToken ct = default)
    {
        await EnsureBrowserAsync();

        var results = new List<RenderedSlide>();
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1080, Height = 1080 }
        });

        try
        {
            for (var i = 0; i < request.Slides.Count; i++)
            {
                var slide = request.Slides[i];
                var html = BuildSlideHtml(slide, request.Author);

                await page.SetContentAsync(html, new PageSetContentOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                var pngBytes = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    Clip = new Clip { X = 0, Y = 0, Width = 1080, Height = 1080 }
                });

                var fileName = $"slide-{i + 1:D2}-{slide.Layout}.png";
                results.Add(new RenderedSlide(i + 1, fileName, pngBytes));

                _logger.LogInformation("Rendered slide {Number}/{Total}: {Layout}",
                    i + 1, request.Slides.Count, slide.Layout);
            }
        }
        finally
        {
            await page.CloseAsync();
        }

        return results;
    }

    private string BuildSlideHtml(SlideData slide, string? author)
    {
        var layout = slide.Layout.ToLowerInvariant();
        var templateFile = Path.Combine(_templatesPath, $"{layout}.html");

        if (!File.Exists(templateFile))
        {
            _logger.LogWarning("Template not found: {Template}, falling back to content", templateFile);
            templateFile = Path.Combine(_templatesPath, "content.html");
        }

        var html = File.ReadAllText(templateFile);
        var css = File.ReadAllText(Path.Combine(_templatesPath, "base.css"));

        // Inline the CSS (replace the link tag)
        html = html.Replace(
            "<link rel=\"stylesheet\" href=\"base.css\">",
            $"<style>{css}</style>");

        // Replace placeholders
        html = html.Replace("{{HEADLINE}}", Escape(slide.Headline));
        html = html.Replace("{{BODY}}", Escape(slide.Body ?? ""));
        html = html.Replace("{{SUBTEXT}}", Escape(slide.Subtext ?? ""));
        html = html.Replace("{{CTA}}", Escape(slide.CTA ?? ""));
        html = html.Replace("{{AUTHOR}}", Escape(author ?? ""));
        html = html.Replace("{{SLIDE_NUMBER}}", slide.SlideNumber.ToString());

        // Handle conditional highlight block
        if (!string.IsNullOrEmpty(slide.Highlight))
        {
            html = html.Replace("{{#HIGHLIGHT}}", "");
            html = html.Replace("{{/HIGHLIGHT}}", "");
            html = html.Replace("{{HIGHLIGHT}}", Escape(slide.Highlight));
        }
        else
        {
            html = ConditionalBlockRegex().Replace(html, "");
        }

        return html;
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    [GeneratedRegex(@"\{\{#HIGHLIGHT\}\}.*?\{\{/HIGHLIGHT\}\}", RegexOptions.Singleline)]
    private static partial Regex ConditionalBlockRegex();

    public async ValueTask DisposeAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        GC.SuppressFinalize(this);
    }
}
