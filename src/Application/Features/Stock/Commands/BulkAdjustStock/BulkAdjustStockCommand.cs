using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Stock.Commands.BulkAdjustStock;

// Requires an authenticated user for StockTransaction.UserId, same as AdjustStockCommand.
[Authorize]
public class BulkAdjustStockCommand : IRequest<Result>, ICacheInvalidation, IRequiresOpenClass
{
    Guid? IRequiresOpenClass.ClassId => ClassId;

    public sealed record Line(Guid ItemId, Guid LocationId, int ActualQuantity, int? ExpectedQuantity = null);

    public Guid ClassId { get; init; }

    // One reason for the whole count.
    public AdjustmentReason Reason { get; init; }
    public IReadOnlyList<Line> Lines { get; init; } = [];

    // Same tags as AdjustStockCommand, one location tag per distinct location touched.
    public IReadOnlyCollection<string> Tags =>
        Lines.Select(l => l.LocationId).Distinct()
            .Select(locationId => CacheConstants.BuildTag(ClassId, locationId))
            .Append(CacheConstants.BuildClassTag(ClassId))
            .ToList();
}
