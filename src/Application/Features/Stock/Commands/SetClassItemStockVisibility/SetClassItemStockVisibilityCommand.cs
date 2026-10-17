using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;

namespace skestock.Application.Features.Stock.Commands.SetClassItemStockVisibility;

[Authorize]
public class SetClassItemStockVisibilityCommand : IRequest<Result>, ICacheInvalidation, IRequiresOpenClass
{
    Guid? IRequiresOpenClass.ClassId => ClassId;

    public Guid ClassId { get; init; }
    public Guid ItemId { get; init; }
    public Guid LocationId { get; init; }
    public bool HideWhenZeroStock { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.BuildCoarseTag(),
        CacheConstants.BuildClassTag(ClassId)
    ];
}
