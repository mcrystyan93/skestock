using ClosedXML.Excel;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.OrderLists.Models;
using skestock.Infrastructure.Export;

namespace skestock.Infrastructure.IntegrationTests;

public class OrderListExcelExporterTests
{
    private static OrderListExportModel BuildModel() => new()
    {
        Name = "Comanda Test",
        Note = "O notă",
        ClassName = "Fall 2026",
        Groups =
        [
            new OrderListExportGroup
            {
                CategoryName = "Brutărie",
                Lines = [new OrderListExportLine { ProductName = "Pâine", Quantity = 3, Unit = "buc" }]
            },
            new OrderListExportGroup
            {
                CategoryName = "Alte articole",
                Lines = [new OrderListExportLine { ProductName = "Ambalaje", Quantity = 1 }]
            }
        ]
    };

    private static IReadOnlyList<string> ReadCellText(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Comanda");

        return worksheet.CellsUsed()
            .Select(c => c.GetString())
            .ToList();
    }

    [Test]
    public void ShouldProduceNonEmptyWorkbook()
    {
        var content = new OrderListExcelExporter().Export(BuildModel());

        content.ShouldNotBeEmpty();
    }

    [Test]
    public void ShouldRenderTitleNoteCategoriesAndRomanianHeaders()
    {
        var content = new OrderListExcelExporter().Export(BuildModel());

        var cells = ReadCellText(content);

        cells.ShouldContain("Comanda Test");
        cells.ShouldContain("Notă: O notă");
        cells.ShouldContain("Brutărie");
        cells.ShouldContain("Alte articole");
        cells.ShouldContain("Produs");
        cells.ShouldContain("Cantitate");
        cells.ShouldContain("U.M.");
        cells.ShouldContain("Observații");
        cells.ShouldContain("Pâine");
        cells.Count(c => c == "Produs").ShouldBe(1);
        cells.Count(c => c == "Cantitate").ShouldBe(1);
        cells.Count(c => c == "U.M.").ShouldBe(1);
        cells.Count(c => c == "Observații").ShouldBe(1);
    }

    [Test]
    public void ShouldOmitNoteRowWhenNoteEmpty()
    {
        var model = BuildModel() with { Note = null };

        var content = new OrderListExcelExporter().Export(model);

        var cells = ReadCellText(content);

        cells.ShouldNotContain(c => c.StartsWith("Notă:", StringComparison.Ordinal));
    }
}
