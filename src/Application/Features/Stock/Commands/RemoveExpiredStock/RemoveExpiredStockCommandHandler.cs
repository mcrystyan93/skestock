using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Events.Stock;

namespace skestock.Application.Features.Stock.Commands.RemoveExpiredStock;

public class RemoveExpiredStockCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<RemoveExpiredStockCommand, Result>
{
    public async ValueTask<Result> Handle(
        RemoveExpiredStockCommand request,
        CancellationToken cancellationToken)
    {
        var identityId = Guard.Against.Null(
            user.Id,
            message: "Removing expired stock requires an authenticated user.");

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var isPerishable = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.Id == request.ItemId)
            .Select(i => i.IsPerishable)
            .SingleOrDefaultAsync(cancellationToken);

        var expiredBatches = await dbContext.StockBatches
            .Where(b => b.ItemId == request.ItemId
                        && b.LocationId == request.LocationId
                        && b.ReceivedClassId == request.ClassId
                        && b.Quantity > 0
                        && isPerishable
                        && b.ExpiryDate.HasValue
                        && b.ExpiryDate.Value <= today)
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        if (expiredBatches.Count == 0)
        {
            return Result.Fail(new StockErrors.NoExpiredQuantity(
                request.ClassId,
                request.ItemId,
                request.LocationId));
        }

        foreach (var batch in expiredBatches)
        {
            var removedQuantity = batch.Quantity;
            batch.Quantity = 0;

            dbContext.StockTransactions.Add(new StockTransaction
            {
                ItemId = request.ItemId,
                LocationId = request.LocationId,
                Batch = batch,
                ClassId = request.ClassId,
                UserId = identityId,
                Type = StockTransactionType.Adjustment,
                QuantityChange = -removedQuantity,
                Reason = nameof(AdjustmentReason.Expired)
            });
        }

        expiredBatches[0].AddDomainEvent(new StockAdjustedEvent(
            request.ClassId,
            request.LocationId));

        // The StockBatch rowversion token turns a concurrent change to one of these expired
        // batches into a DbUpdateConcurrencyException here instead of a lost update.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail(new StockErrors.ConcurrencyConflict(
                request.ClassId,
                request.ItemId,
                request.LocationId));
        }

        return Result.Ok();
    }
}
