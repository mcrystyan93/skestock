using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;

namespace skestock.Application.Features.Stock.Commands.RemoveExpiredStock;

[Authorize]
public class RemoveExpiredStockCommand : IRequest<Result>, ICacheInvalidation
{
    public Guid ClassId { get; init; }
    public Guid ItemId { get; init; }
    public Guid LocationId { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.BuildTag(ClassId, LocationId),
        CacheConstants.BuildClassTag(ClassId)
    ];
}
