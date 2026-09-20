using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Events.Stock;

namespace skestock.Application.Features.Stock.Commands.MoveStock;

public class MoveStockCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<MoveStockCommand, Result>
{
    public async ValueTask<Result> Handle(
        MoveStockCommand request,
        CancellationToken cancellationToken)
    {
        var identityId = Guard.Against.Null(
            user.Id,
            message: "Moving stock requires an authenticated user.");

        if (request.SourceLocationId == request.DestinationLocationId)
            return Result.Fail("Source and destination locations must be different.");

        if (request.Quantity <= 0)
            return Result.Fail("The quantity to move must be greater than zero.");

        // Re-read the class-scoped source batches immediately before changing them. The validator
        // protects the normal pipeline path, while this second read prevents a stale validation
        // result from allowing a partial transfer after another write changed the source total.
        var sourceBatches = await dbContext.StockBatches
            .Where(b => b.ItemId == request.ItemId
                        && b.LocationId == request.SourceLocationId
                        && b.ReceivedClassId == request.ClassId
                        && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        var sourceTotal = sourceBatches.Sum(b => b.Quantity);
        if (request.Quantity > sourceTotal)
            return Result.Fail(new StockErrors.InsufficientQuantity(
                request.ItemId,
                request.SourceLocationId,
                request.Quantity,
                sourceTotal));

        var remainingToMove = request.Quantity;
        var destinationBatches = new List<StockBatch>();

        foreach (var sourceBatch in sourceBatches)
        {
            if (remainingToMove == 0)
                break;

            var movedFromBatch = Math.Min(sourceBatch.Quantity, remainingToMove);
            sourceBatch.Quantity -= movedFromBatch;
            remainingToMove -= movedFromBatch;

            var destinationBatch = new StockBatch
            {
                ItemId = sourceBatch.ItemId,
                LocationId = request.DestinationLocationId,
                ReceivedClassId = sourceBatch.ReceivedClassId,
                Quantity = movedFromBatch,
                ExpiryDate = sourceBatch.ExpiryDate,
                ReceivedDate = sourceBatch.ReceivedDate,
                UnitPrice = sourceBatch.UnitPrice,
                GoodsReceiptId = sourceBatch.GoodsReceiptId
            };

            destinationBatches.Add(destinationBatch);

            dbContext.StockTransactions.Add(new StockTransaction
            {
                ItemId = sourceBatch.ItemId,
                LocationId = request.SourceLocationId,
                Batch = sourceBatch,
                ClassId = request.ClassId,
                UserId = identityId,
                Type = StockTransactionType.Transfer,
                QuantityChange = -movedFromBatch,
                Reason = null,
                GoodsReceiptId = null
            });

            dbContext.StockTransactions.Add(new StockTransaction
            {
                ItemId = destinationBatch.ItemId,
                LocationId = request.DestinationLocationId,
                Batch = destinationBatch,
                ClassId = request.ClassId,
                UserId = identityId,
                Type = StockTransactionType.Transfer,
                QuantityChange = movedFromBatch,
                Reason = null,
                GoodsReceiptId = null
            });
        }

        // The quantity check above guarantees that the FIFO walk resolves the full request. Keep
        // this guard as a defensive invariant in case the source data is changed unexpectedly
        // between materialization and the walk.
        if (remainingToMove != 0)
            return Result.Fail(new StockErrors.InsufficientQuantity(
                request.ItemId,
                request.SourceLocationId,
                request.Quantity,
                request.Quantity - remainingToMove));

        dbContext.StockBatches.AddRange(destinationBatches);
        sourceBatches[0].AddDomainEvent(new StockMovedEvent(
            request.ClassId,
            request.ItemId,
            request.SourceLocationId,
            request.DestinationLocationId,
            request.Quantity));

        // One SaveChangesAsync persists all decrements, destination rows and paired transactions
        // together. Relational providers wrap the complete change set in one transaction. The
        // source-batch rowversion token turns a concurrent draw-down into a
        // DbUpdateConcurrencyException here rather than a lost update.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail(new StockErrors.ConcurrencyConflict(
                request.ClassId,
                request.ItemId,
                request.SourceLocationId));
        }

        return Result.Ok();
    }
}
