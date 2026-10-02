using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Enums;
using skestock.Domain.Events.Stock;

namespace skestock.Application.Features.Stock.Commands.ExtendExpiredStockExpiry;

public class ExtendExpiredStockExpiryCommandHandler(IApplicationDbContext dbContext, IUser user, TimeProvider timeProvider)
    : IRequestHandler<ExtendExpiredStockExpiryCommand, Result>
{
    public const string ReasonPrefix = "ExpiryExtended";

    public async ValueTask<Result> Handle(
        ExtendExpiredStockExpiryCommand request,
        CancellationToken cancellationToken)
    {
        var identityId = Guard.Against.Null(
            user.Id,
            message: "Extending expired stock requires an authenticated user.");

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var newExpiry = today.AddDays(request.ExtensionDays);

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
            var previousExpiry = batch.ExpiryDate!.Value;
            batch.ExpiryDate = newExpiry;

            dbContext.StockTransactions.Add(new StockTransaction
            {
                ItemId = request.ItemId,
                LocationId = request.LocationId,
                Batch = batch,
                ClassId = request.ClassId,
                UserId = identityId,
                Type = StockTransactionType.Adjustment,
                QuantityChange = 0,
                Reason = $"{ReasonPrefix}: {previousExpiry:yyyy-MM-dd} -> {newExpiry:yyyy-MM-dd}"
            });
        }

        expiredBatches[0].AddDomainEvent(new StockAdjustedEvent(request.ClassId, request.LocationId));

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
