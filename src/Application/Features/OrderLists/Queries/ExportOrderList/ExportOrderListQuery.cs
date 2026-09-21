using skestock.Application.Common.Models;

namespace skestock.Application.Features.OrderLists.Queries.ExportOrderList;

public class ExportOrderListQuery : IRequest<Result<FileExportResult>>
{
    public Guid Id { get; init; }
}
