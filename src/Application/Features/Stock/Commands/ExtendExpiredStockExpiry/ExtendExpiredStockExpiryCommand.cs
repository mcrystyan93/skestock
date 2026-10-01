using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;

namespace skestock.Application.Features.Stock.Commands.ExtendExpiredStockExpiry;

[Authorize]
public class ExtendExpiredStockExpiryCommand : IRequest<Result>, ICacheInvalidation
{
    public const int MaxExtensionDays = 365;

    public Guid ClassId { get; init; }
    public Guid ItemId { get; init; }
    public Guid LocationId { get; init; }
    public int ExtensionDays { get; init; }

    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.BuildTag(ClassId, LocationId),
        CacheConstants.BuildClassTag(ClassId)
    ];
}
