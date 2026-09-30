using System.Globalization;
using System.Reflection;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;
using SkiaSharp;

namespace skestock.Infrastructure.Export;

// Renders an order list into a single tall PNG mirroring the Excel layout: title and note header,
// then each category as a shaded header row followed by its Romanian-labelled line table.
// Fonts are embedded because the runtime image ships none.
public class OrderListImageExporter : IOrderListImageExporter
{
    private const float Scale = 2f;
    private const float Width = 1000f;
    private const float Margin = 24f;
    private const float CellPadding = 8f;
    private const float FontSize = 15f;
    private const float TitleSize = 22f;
    private const float LineHeight = 20f;

    private static readonly float[] ColumnWeights = [40f, 12f, 10f, 40f];
    private static readonly string[] Headers = ["Produs", "Cantitate", "U.M.", "Observații"];

    private static readonly Lazy<SKTypeface> Regular = new(() => LoadTypeface("LiberationSans-Regular.ttf"));
    private static readonly Lazy<SKTypeface> Bold = new(() => LoadTypeface("LiberationSans-Bold.ttf"));

    public byte[] Export(OrderListExportModel model)
    {
        var contentWidth = Width - 2 * Margin;
        var totalWeight = ColumnWeights.Sum();
        var columnWidths = ColumnWeights.Select(w => contentWidth * w / totalWeight).ToArray();
        var columnX = new float[columnWidths.Length];
        for (var i = 1; i < columnX.Length; i++)
            columnX[i] = columnX[i - 1] + columnWidths[i - 1];

        using var regular = new SKFont(Regular.Value, FontSize);
        using var bold = new SKFont(Bold.Value, FontSize);
        using var title = new SKFont(Bold.Value, TitleSize);

        var rows = new List<Row>();

        if (!string.IsNullOrWhiteSpace(model.Name))
            rows.Add(new Row(RowKind.Title, [Wrap(model.Name, title, contentWidth)]));

        if (!string.IsNullOrWhiteSpace(model.Note))
            rows.Add(new Row(RowKind.Note, [Wrap($"Notă: {model.Note}", regular, contentWidth)]));

        if (rows.Count > 0)
            rows.Add(new Row(RowKind.Spacer, []));

        if (model.Groups.Count > 0)
            rows.Add(new Row(RowKind.TableHeader, Headers.Select(h => new[] { h }).ToArray()));

        foreach (var group in model.Groups)
        {
            rows.Add(new Row(RowKind.Category, [Wrap(group.CategoryName, bold, contentWidth - 2 * CellPadding)]));

            foreach (var line in group.Lines)
            {
                rows.Add(new Row(RowKind.Line,
                [
                    Wrap(line.ProductName, regular, columnWidths[0] - 2 * CellPadding),
                    [line.Quantity.ToString("0.##", CultureInfo.CurrentCulture)],
                    Wrap(line.Unit ?? string.Empty, regular, columnWidths[2] - 2 * CellPadding),
                    Wrap(line.Notes ?? string.Empty, regular, columnWidths[3] - 2 * CellPadding)
                ]));
            }

            rows.Add(new Row(RowKind.Spacer, []));
        }

        var height = 2 * Margin + rows.Sum(r => r.Height);

        using var bitmap = new SKBitmap((int)(Width * Scale), (int)(Math.Ceiling(height) * Scale));
        using var canvas = new SKCanvas(bitmap);
        canvas.Scale(Scale);
        canvas.Clear(SKColors.White);

        using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var fillPaint = new SKPaint { Color = new SKColor(0xD3, 0xD3, 0xD3) };
        using var linePaint = new SKPaint { Color = new SKColor(0xBF, 0xBF, 0xBF), StrokeWidth = 1f };

        var y = Margin;
        foreach (var row in rows)
        {
            switch (row.Kind)
            {
                case RowKind.Title:
                    DrawLines(canvas, row.Cells[0], title, textPaint, Margin, y, 0);
                    break;
                case RowKind.Note:
                    DrawLines(canvas, row.Cells[0], regular, textPaint, Margin, y, 0);
                    break;
                case RowKind.Category:
                    canvas.DrawRect(Margin, y, contentWidth, row.Height, fillPaint);
                    DrawLines(canvas, row.Cells[0], bold, textPaint, Margin + CellPadding, y, CellPadding / 2);
                    break;
                case RowKind.TableHeader:
                case RowKind.Line:
                    var font = row.Kind == RowKind.TableHeader ? bold : regular;
                    for (var c = 0; c < row.Cells.Length; c++)
                        DrawLines(canvas, row.Cells[c], font, textPaint, Margin + columnX[c] + CellPadding, y, CellPadding / 2);
                    canvas.DrawLine(Margin, y + row.Height, Margin + contentWidth, y + row.Height, linePaint);
                    break;
            }

            y += row.Height;
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static void DrawLines(SKCanvas canvas, string[] lines, SKFont font, SKPaint paint, float x, float y,
        float topPadding)
    {
        var baseline = y + topPadding + LineHeight - 5f;
        foreach (var line in lines)
        {
            canvas.DrawText(line, x, baseline, SKTextAlign.Left, font, paint);
            baseline += LineHeight;
        }
    }

    private static string[] Wrap(string text, SKFont font, float maxWidth)
    {
        var result = new List<string>();

        foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
        {
            var current = string.Empty;

            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : $"{current} {word}";
                if (font.MeasureText(candidate) <= maxWidth)
                {
                    current = candidate;
                    continue;
                }

                if (current.Length > 0)
                    result.Add(current);

                current = word;

                // A single word wider than the column is broken by character.
                while (font.MeasureText(current) > maxWidth && current.Length > 1)
                {
                    var fit = 1;
                    while (fit < current.Length && font.MeasureText(current[..(fit + 1)]) <= maxWidth)
                        fit++;

                    result.Add(current[..fit]);
                    current = current[fit..];
                }
            }

            result.Add(current);
        }

        return [.. result];
    }

    private static SKTypeface LoadTypeface(string fileName)
    {
        var assembly = typeof(OrderListImageExporter).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        return SKTypeface.FromStream(stream) ?? throw new InvalidOperationException($"Font {fileName} could not be loaded.");
    }

    private enum RowKind { Title, Note, Spacer, TableHeader, Category, Line }

    private sealed record Row(RowKind Kind, string[][] Cells)
    {
        public float Height => Kind == RowKind.Spacer
            ? LineHeight
            : Math.Max(1, Cells.Max(c => c.Length)) * LineHeight + (Kind is RowKind.Title or RowKind.Note ? 0 : CellPadding);
    }
}
