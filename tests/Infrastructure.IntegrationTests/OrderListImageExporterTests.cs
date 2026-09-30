using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.OrderLists.Models;
using skestock.Infrastructure.Export;
using SkiaSharp;

namespace skestock.Infrastructure.IntegrationTests;

public class OrderListImageExporterTests
{
    private readonly OrderListImageExporter _exporter = new();

    private static OrderListExportModel BuildModel(int lines = 1, string? notes = null) => new()
    {
        Name = "Comanda Test",
        Note = "O notă: ăâîșț",
        Groups =
        [
            new OrderListExportGroup
            {
                CategoryName = "Brutărie",
                Lines = Enumerable.Range(0, lines)
                    .Select(i => new OrderListExportLine
                    {
                        ProductName = $"Pâine {i}", Quantity = 3, Unit = "buc", Notes = notes
                    })
                    .ToList()
            },
            new OrderListExportGroup
            {
                CategoryName = "Alte articole",
                Lines = [new OrderListExportLine { ProductName = "Ambalaje", Quantity = 1 }]
            }
        ]
    };

    private static SKBitmap Decode(byte[] png) => SKBitmap.Decode(png).ShouldNotBeNull();

    [Test]
    public void Export_ProducesValidPng()
    {
        var bytes = _exporter.Export(BuildModel());

        bytes.Take(8).ShouldBe([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        using var bitmap = Decode(bytes);
        bitmap.Width.ShouldBe(2000);
        bitmap.Height.ShouldBeGreaterThan(0);
    }

    [Test]
    public void Export_MoreLines_ProducesTallerImage()
    {
        using var small = Decode(_exporter.Export(BuildModel(1)));
        using var large = Decode(_exporter.Export(BuildModel(10)));

        large.Height.ShouldBeGreaterThan(small.Height);
    }

    [Test]
    public void Export_LongNotesAndWords_WrapAndGrowRow()
    {
        var longNote = string.Join(' ', Enumerable.Repeat("observație foarte lungă", 20)) + new string('x', 200);

        using var plain = Decode(_exporter.Export(BuildModel()));
        using var wrapped = Decode(_exporter.Export(BuildModel(notes: longNote)));

        wrapped.Height.ShouldBeGreaterThan(plain.Height);
    }

    [Test]
    public void Export_EmptyModel_DoesNotThrow()
    {
        using var bitmap = Decode(_exporter.Export(new OrderListExportModel()));

        bitmap.Height.ShouldBeGreaterThan(0);
    }
}
