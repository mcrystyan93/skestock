using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Stock.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

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
        var delta = request.ActualQuantity - currentTotal;
        
        // delta == 0 is rejected by the validator (NoAdjustmentNeeded) before the handler runs,
        // so only the shortfall/surplus branches below are ever reached here.
        if (delta < 0)
        {
            var remainingToConsume = -delta;

            // Walk the oldest-expiry-first batches, taking as much as available from each one
            // until the full deficit is accounted for. ActualQuantity >= 0 combined with
            // currentTotal being the sum of these same (Quantity > 0) batches guarantees
            // remainingToConsume can never exceed the total available, so the loop always
            // fully resolves the deficit.
            foreach (var batch in batches)
            {
                if (remainingToConsume <= 0)
                    break;

                var takenFromBatch = Math.Min(batch.Quantity, remainingToConsume);
                batch.Quantity -= takenFromBatch;
                remainingToConsume -= takenFromBatch;

                dbContext.StockTransactions.Add(new StockTransaction
                {
                    ItemId = request.ItemId,
                    LocationId = request.LocationId,
                    Batch = batch,
                    ClassId = request.ClassId,
                    UserId = identityId,
                    Type = StockTransactionType.Adjustment,
                    QuantityChange = -takenFromBatch,
                    Reason = request.Reason.ToString()
                });
            }
        }
        else
        {
            // Surplus: nobody logged this stock before, so there's no existing batch to
            // attribute it to - create a new one instead. Price/expiry are genuinely unknown for
            // stock discovered this way, so they're set to their simplest defaults rather than
            // guessed at (unlike a GoodsReceipt line, which requires a real expiry/price).
            var surplusBatch = new StockBatch
            {
                ItemId = request.ItemId,
                LocationId = request.LocationId,
                ReceivedClassId = request.ClassId,
                Quantity = delta,
                ExpiryDate = null,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                UnitPrice = 0,
                GoodsReceiptId = null
            };

            dbContext.StockBatches.Add(surplusBatch);

            dbContext.StockTransactions.Add(new StockTransaction
            {
                ItemId = request.ItemId,
                LocationId = request.LocationId,
                Batch = surplusBatch,
                ClassId = request.ClassId,
                UserId = identityId,
                Type = StockTransactionType.Adjustment,
                QuantityChange = delta,
                Reason = request.Reason.ToString()
            });
        }

        // A single SaveChangesAsync wraps every batch update + new transaction row in one
        // implicit DB transaction - if anything fails, nothing partially saves.
        await dbContext.SaveChangesAsync(cancellationToken);

        var item = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.Id == request.ItemId)
            .Select(i => new { i.Name, i.Unit, i.IsPerishable, i.MinThreshold })
            .SingleAsync(cancellationToken);

        var location = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.Id == request.LocationId)
            .Select(l => l.Name)
            .SingleAsync(cancellationToken);

        var dto = new StockItemDto
        {
            ItemId = request.ItemId,
            ItemName = item.Name,
            LocationId = request.LocationId,
            LocationName = location,
            Unit = item.Unit,
            IsPerishable = item.IsPerishable,
            Quantity = request.ActualQuantity,
            IsLowStock = request.ActualQuantity < item.MinThreshold
        };

        return Result.Ok(dto);
    }
}
