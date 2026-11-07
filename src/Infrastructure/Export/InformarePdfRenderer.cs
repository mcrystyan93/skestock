using skestock.Application.Features.FruitDessertPlans.Abstractions;
using SkiaSharp;

namespace skestock.Infrastructure.Export;

public class InformarePdfRenderer : IInformarePdfRenderer
{
    private const float PageWidth = 595f; // A4 in points
    private const float PageHeight = 842f;
    private const float Margin = 56f;
    private const float TitleSize = 16f;
    private const float BodySize = 12f;
    private const float TitleLineHeight = 22f;
    private const float LineHeight = 17f;

    public byte[] Render(string title, string body)
    {
        using var stream = new SKDynamicMemoryWStream();
        using (var document = SKDocument.CreatePdf(stream))
        {
            using var titleFont = new SKFont(OrderListImageExporter.Bold.Value, TitleSize) { Subpixel = true };
            using var bodyFont = new SKFont(OrderListImageExporter.Regular.Value, BodySize) { Subpixel = true };
            using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            var width = PageWidth - 2 * Margin;

            SKCanvas? canvas = null;
            var y = 0f;

            void NewPage()
            {
                if (canvas is not null) document.EndPage();
                canvas = document.BeginPage(PageWidth, PageHeight);
                y = Margin;
            }

            void Draw(string[] lines, SKFont font, float lineHeight)
            {
                foreach (var line in lines)
                {
                    if (canvas is null || y + lineHeight > PageHeight - Margin) NewPage();
                    canvas!.DrawText(line, Margin, y + lineHeight - 5f, SKTextAlign.Left, font, paint);
                    y += lineHeight;
                }
            }

            NewPage();
            Draw(OrderListImageExporter.Wrap(title, titleFont, width), titleFont, TitleLineHeight);
            y += LineHeight;
            // shortcut: the body is WhatsApp markup (*bold*, _italic_) drawn as plain text, upgrade when the template needs real formatting.
            Draw(OrderListImageExporter.Wrap(body, bodyFont, width), bodyFont, LineHeight);

            document.EndPage();
            document.Close();
        }

        using var data = stream.DetachAsData();
        return data.ToArray();
    }
}
