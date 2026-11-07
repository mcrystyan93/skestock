using System.Text;
using NUnit.Framework;
using Shouldly;
using skestock.Infrastructure.Export;
using SkiaSharp;

namespace skestock.Infrastructure.IntegrationTests;

public class InformarePdfRendererTests
{
    private readonly InformarePdfRenderer _renderer = new();

    [Test]
    public void Render_ReturnsPdfBytes()
    {
        var bytes = _renderer.Render("Informare fructe și desert", "Bună ziua, Maria!\nVă mulțumim!");

        Encoding.ASCII.GetString(bytes, 0, 4).ShouldBe("%PDF");
    }

    [Test]
    public void Render_LongMultiParagraphText_ProducesValidPdfOnSeveralPages()
    {
        var body = string.Join("\n\n", Enumerable.Range(0, 200).Select(i => $"Paragraful {i}: ăâîșț lorem ipsum dolor sit amet."));

        var bytes = _renderer.Render("Titlu", body);

        Encoding.ASCII.GetString(bytes, 0, 4).ShouldBe("%PDF");
        // Each page object is emitted as "/Type /Page" (the page tree is "/Type /Pages").
        Encoding.Latin1.GetString(bytes).Split("/Type /Page\n").Length.ShouldBeGreaterThan(2);
    }

    [TestCase("LiberationSans-Regular.ttf")]
    [TestCase("LiberationSans-Bold.ttf")]
    public void EmbeddedFont_ContainsRomanianGlyphs(string fileName)
    {
        var assembly = typeof(InformarePdfRenderer).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var typeface = SKTypeface.FromStream(stream);
        using var font = new SKFont(typeface);

        font.ContainsGlyphs("ăâîșțĂÂÎȘȚ").ShouldBeTrue();
    }
}
