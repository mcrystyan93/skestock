using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Stock.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Enums;
using skestock.Domain.Events.Stock;

namespace skestock.Application.Features.Stock.Commands.AdjustStock;

public class AdjustStockCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<AdjustStockCommand, Result<StockItemDto>>
{
    public async ValueTask<Result<StockItemDto>> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        // AuthorizationBehaviour (earlier in the pipeline) guarantees an authenticated caller, so
        // IUser.Id is expected to be populated. StockTransaction.UserId is a FK to
        // UserProfile.IdentityId (not UserProfile.Id), so the raw identity id can be stored
        // directly - same rationale as CreateGoodsReceiptCommandHandler.
        var identityId = Guard.Against.Null(user.Id, message: "Adjusting stock requires an authenticated user.");

        // Only batches that still have stock left can be drawn down against - already-depleted
        // batches (Quantity == 0) are excluded from both the sum and the FIFO walk below.
        var batches = await dbContext.StockBatches
            .Where(b => b.ItemId == request.ItemId
                        && b.LocationId == request.LocationId
                        && b.ReceivedClassId == request.ClassId
                        && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        var currentTotal = batches.Sum(b => b.Quantity);
        // delta == 0 is rejected by the validator (NoAdjustmentNeeded) before the handler runs,
        // so the helper always touches a batch here.
        var anchor = StockAdjuster.Apply(
            dbContext, batches, request.ClassId, request.ItemId, request.LocationId,
            request.ActualQuantity, request.Reason, identityId);
        anchor?.AddDomainEvent(new StockAdjustedEvent(request.ClassId, request.LocationId));

        // A single SaveChangesAsync wraps every batch update + new transaction row in one
        // implicit DB transaction - if anything fails, nothing partially saves. The StockBatch
        // rowversion token means a concurrent draw-down that changed one of these batches first
        // makes this UPDATE match zero rows, raising DbUpdateConcurrencyException instead of
        // silently overwriting the other operation's result.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail<StockItemDto>(new StockErrors.ConcurrencyConflict(
                request.ClassId,
                request.ItemId,
                request.LocationId));
        }

        var item = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.Id == request.ItemId)
            .Select(i => new { i.Name, i.Unit, i.IsPerishable })
            .SingleAsync(cancellationToken);

        var threshold = await dbContext.ItemLocationThresholds
            .AsNoTracking()
            .Where(t => t.ItemId == request.ItemId && t.LocationId == request.LocationId)
            .Select(t => (int?)t.MinThreshold)
            .SingleOrDefaultAsync(cancellationToken);

        var location = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.Id == request.LocationId)
            .Select(l => l.Name)
            .SingleAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var expiredQuantity = item.IsPerishable
            ? batches
                .Where(b => b.Quantity > 0
                            && b.ExpiryDate.HasValue
                            && b.ExpiryDate.Value <= today)
                .Sum(b => b.Quantity)
            : 0;

        var dto = new StockItemDto
        {
            ItemId = request.ItemId,
            ItemName = item.Name,
            LocationId = request.LocationId,
            LocationName = location,
            Unit = item.Unit,
            IsPerishable = item.IsPerishable,
            IsExpired = expiredQuantity > 0,
            ExpiredQuantity = expiredQuantity,
            Quantity = request.ActualQuantity,
            IsLowStock = request.ActualQuantity < threshold
        };

        return Result.Ok(dto);
    }
}
