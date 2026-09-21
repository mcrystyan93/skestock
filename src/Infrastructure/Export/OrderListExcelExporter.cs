using ClosedXML.Excel;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Infrastructure.Export;

// Renders an order list into a single-worksheet .xlsx: a title (list name) and note header,
// then each category as a header row followed by its Romanian-labelled line table.
public class OrderListExcelExporter : IOrderListExcelExporter
{
    private const int ColumnCount = 4;

    public byte[] Export(OrderListExportModel model)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Comanda");

        var row = 1;

        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            var title = worksheet.Range(row, 1, row, ColumnCount).Merge();
            title.Value = model.Name;
            title.Style.Font.Bold = true;
            title.Style.Font.FontSize = 14;
            row++;
        }

        if (!string.IsNullOrWhiteSpace(model.Note))
        {
            var note = worksheet.Range(row, 1, row, ColumnCount).Merge();
            note.Value = $"Notă: {model.Note}";
            note.Style.Alignment.WrapText = true;
            row++;
        }

        // Spacer row between the header block and the grouped table.
        row++;

        if (model.Groups.Count > 0)
        {
            worksheet.Cell(row, 1).Value = "Produs";
            worksheet.Cell(row, 2).Value = "Cantitate";
            worksheet.Cell(row, 3).Value = "U.M.";
            worksheet.Cell(row, 4).Value = "Observații";
            worksheet.Range(row, 1, row, ColumnCount).Style.Font.Bold = true;
            row++;
        }

        foreach (var group in model.Groups)
        {
            var header = worksheet.Range(row, 1, row, ColumnCount).Merge();
            header.Value = group.CategoryName;
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;

            foreach (var line in group.Lines)
            {
                worksheet.Cell(row, 1).Value = line.ProductName;
                worksheet.Cell(row, 2).Value = line.Quantity;
                worksheet.Cell(row, 3).Value = line.Unit ?? string.Empty;
                worksheet.Cell(row, 4).Value = line.Notes ?? string.Empty;
                row++;
            }

            // Spacer row between groups.
            row++;
        }

        worksheet.Column(1).Width = 40;
        worksheet.Column(2).Width = 12;
        worksheet.Column(3).Width = 10;
        worksheet.Column(4).Width = 40;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
