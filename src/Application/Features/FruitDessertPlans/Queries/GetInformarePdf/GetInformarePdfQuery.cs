using skestock.Application.Common.Models;

namespace skestock.Application.Features.FruitDessertPlans.Queries.GetInformarePdf;

// Read-only and not ICacheableQuery: a template edit must not leave a stale PDF behind.
public record GetInformarePdfQuery : IRequest<Result<FileExportResult>>
{
    public Guid ClassId { get; init; }
    public Guid EntryId { get; init; }
}
