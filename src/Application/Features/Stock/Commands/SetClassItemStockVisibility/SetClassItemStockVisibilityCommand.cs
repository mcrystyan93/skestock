using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;

namespace skestock.Application.Features.Stock.Commands.SetClassItemStockVisibility;

[Authorize]
public class SetClassItemStockVisibilityCommand : IRequest<Result>, ICacheInvalidation
{
    public Guid ClassId { get; init; }
    public Guid ItemId { get; init; }
    public bool HideWhenZeroStock { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.BuildCoarseTag(),
        CacheConstants.BuildClassTag(ClassId)
    ];
}
