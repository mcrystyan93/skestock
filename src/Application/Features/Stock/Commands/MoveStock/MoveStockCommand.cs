using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;

namespace skestock.Application.Features.Stock.Commands.MoveStock;

[Authorize]
public class MoveStockCommand : IRequest<Result>, ICacheInvalidation
{
    public Guid ClassId { get; init; }
    public Guid ItemId { get; init; }
    public Guid SourceLocationId { get; init; }
    public Guid DestinationLocationId { get; init; }
    public int Quantity { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.BuildCoarseTag(),
        CacheConstants.BuildClassTag(ClassId),
        CacheConstants.BuildTag(ClassId, SourceLocationId),
        CacheConstants.BuildTag(ClassId, DestinationLocationId),
        StockBatches.CacheConstants.StockBatchListTag
    ];
}
