using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Domain.Constants;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.SchoolClasses.Commands.CloseSchoolClass;

[Authorize(Roles = Roles.Administrator)]
public sealed class CloseSchoolClassCommand : IRequest<Result>, ICacheInvalidation
{
    public Guid Id { get; init; }
    public Guid TargetClassId { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.SchoolClassListTag,
        StockCacheConstants.BuildCoarseTag(),
        StockCacheConstants.BuildClassTag(Id),
        StockCacheConstants.BuildClassTag(TargetClassId),
        StockBatches.CacheConstants.StockBatchListTag
    ];
}
