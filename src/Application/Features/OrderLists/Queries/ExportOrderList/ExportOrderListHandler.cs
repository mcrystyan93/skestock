using System.Globalization;
using System.IO;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.OrderLists.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.OrderLists.Queries.ExportOrderList;

public class ExportOrderListHandler(IApplicationDbContext dbContext, IOrderListExcelExporter exporter)
    : IRequestHandler<ExportOrderListQuery, Result<FileExportResult>>
{
    private const string UncategorizedGroupName = "Alte articole";

    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async ValueTask<Result<FileExportResult>> Handle(
        ExportOrderListQuery request, CancellationToken cancellationToken)
    {
        var data = await dbContext.OrderLists
            .AsNoTracking()
            .Where(o => o.Id == request.Id)
            .Select(o => new
            {
                o.Name,
                o.Note,
                ClassName = o.Class != null ? o.Class.Name : null,
                o.Status,
                o.SubmittedAt,
                Lines = o.Lines.Select(l => new
                {
                    l.ItemId,
                    l.ProductName,
                    l.Quantity,
                    l.Unit,
                    l.Notes,
                    CategoryName = l.Item != null ? l.Item.Category.Name : null
                }).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (data is null)
            return Result.Fail(new OrderListErrors.OrderListNotFound(request.Id));

        if (data.Status != OrderListStatus.Submitted)
            return Result.Fail(new OrderListErrors.OrderListNotExportable(request.Id, data.Status.ToString()));

        var groups = data.Lines
            .GroupBy(l => l.ItemId is null ? UncategorizedGroupName : l.CategoryName ?? UncategorizedGroupName)
            .Select(g => new OrderListExportGroup
            {
                CategoryName = g.Key,
                Lines = g
                    .OrderBy(l => l.ProductName, StringComparer.CurrentCultureIgnoreCase)
                    .Select(l => new OrderListExportLine
                    {
                        ProductName = l.ProductName,
                        Quantity = l.Quantity,
                        Unit = l.Unit,
                        Notes = l.Notes
                    })
                    .ToList()
            })
            .OrderBy(g => g.CategoryName == UncategorizedGroupName)
            .ThenBy(g => g.CategoryName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var model = new OrderListExportModel
        {
            Name = data.Name,
            Note = data.Note,
            ClassName = data.ClassName,
            SubmittedAt = data.SubmittedAt,
            Groups = groups
        };

        var content = exporter.Export(model);
        var fileName = BuildFileName(data.Name, data.ClassName, data.SubmittedAt);

        return Result.Ok(new FileExportResult
        {
            Content = content,
            FileName = fileName,
            ContentType = XlsxContentType
        });
    }

    private static string BuildFileName(string? name, string? className, DateTimeOffset? submittedAt)
    {
        var baseName = !string.IsNullOrWhiteSpace(name)
            ? name
            : $"Comanda-{(string.IsNullOrWhiteSpace(className) ? "comanda" : className)}-" +
              $"{(submittedAt ?? DateTimeOffset.UtcNow).ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";

        var sanitized = string.Concat(baseName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c))
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "comanda";

        return $"{sanitized}.xlsx";
    }
}
