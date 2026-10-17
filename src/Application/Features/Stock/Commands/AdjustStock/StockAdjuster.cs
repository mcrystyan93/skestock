using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Stock.Commands.AdjustStock;

// Shared by AdjustStockCommandHandler and BulkAdjustStockCommandHandler so a single recount and
// a bulk recount reconcile stock identically.
internal static class StockAdjuster
{
    /// <summary>
    /// Reconciles <paramref name="actualQuantity"/> against the given batches (Quantity &gt; 0, same
    /// item/location/class, already ordered oldest expiry first). A shortfall is drawn down FIFO with one
    /// adjustment transaction per touched batch; a surplus adds a new unattributed batch. Returns the
    /// entity to attach the StockAdjustedEvent to, or null when the count already matches.
    /// </summary>
    public static StockBatch? Apply(
        IApplicationDbContext dbContext,
        IReadOnlyList<StockBatch> batches,
        Guid classId,
        Guid itemId,
        Guid locationId,
        int actualQuantity,
        AdjustmentReason reason,
        Guid userId)
    {
        var delta = actualQuantity - batches.Sum(b => b.Quantity);

        if (delta == 0)
            return null;

        if (delta < 0)
        {
            var remainingToConsume = -delta;

            // Walk the oldest-expiry-first batches, taking as much as available from each one
            // until the full deficit is accounted for. ActualQuantity >= 0 combined with the
            // total being the sum of these same (Quantity > 0) batches guarantees
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
                    ItemId = itemId,
                    LocationId = locationId,
                    Batch = batch,
                    ClassId = classId,
                    UserId = userId,
                    Type = StockTransactionType.Adjustment,
                    QuantityChange = -takenFromBatch,
                    Reason = reason.ToString()
                });
            }

            return batches[0];
        }

        // Surplus: nobody logged this stock before, so there's no existing batch to
        // attribute it to - create a new one instead. Price/expiry are genuinely unknown for
        // stock discovered this way, so they're set to their simplest defaults rather than
        // guessed at (unlike a GoodsReceipt line, which requires a real expiry/price).
        var surplusBatch = new StockBatch
        {
            ItemId = itemId,
            LocationId = locationId,
            ReceivedClassId = classId,
            Quantity = delta,
            ExpiryDate = null,
            ReceivedDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime),
            UnitPrice = 0,
            GoodsReceiptId = null
        };

        dbContext.StockBatches.Add(surplusBatch);

        dbContext.StockTransactions.Add(new StockTransaction
        {
            ItemId = itemId,
            LocationId = locationId,
            Batch = surplusBatch,
            ClassId = classId,
            UserId = userId,
            Type = StockTransactionType.Adjustment,
            QuantityChange = delta,
            Reason = reason.ToString()
        });

        return surplusBatch;
    }
}
