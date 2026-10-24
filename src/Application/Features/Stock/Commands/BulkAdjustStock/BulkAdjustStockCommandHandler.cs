using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Stock.Commands.AdjustStock;
using skestock.Domain.Events.Stock;

namespace skestock.Application.Features.Stock.Commands.BulkAdjustStock;

public class BulkAdjustStockCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<BulkAdjustStockCommand, Result>
{
    public async ValueTask<Result> Handle(BulkAdjustStockCommand request, CancellationToken cancellationToken)
    {
        // Same identity rationale as AdjustStockCommandHandler.
        var identityId = Guard.Against.Null(user.Id, message: "Adjusting stock requires an authenticated user.");

        // One query for every line; the lines' item/location sets over-select the (item, location)
        // pairs slightly (cross product) but the grouping below only reads the requested pairs.
        var itemIds = request.Lines.Select(l => l.ItemId).Distinct().ToList();
        var locationIds = request.Lines.Select(l => l.LocationId).Distinct().ToList();
        var batches = await dbContext.StockBatches
            .Where(b => b.ReceivedClassId == request.ClassId
                        && b.Quantity > 0
                        && itemIds.Contains(b.ItemId)
                        && locationIds.Contains(b.LocationId))
            .ToListAsync(cancellationToken);

        var byPair = batches
            .GroupBy(b => (b.ItemId, b.LocationId))
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Domain.Entities.Stock.StockBatch>)g
                    .OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue)
                    .ThenBy(b => b.Id)
                    .ToList());

        // Reject the whole count if any line's stock moved since the counter saw it; without this the
        // absolute count would silently overwrite a concurrent receipt, move or surplus.
        if (request.Lines.Any(l => l.ExpectedQuantity is { } expected
                                   && expected != (byPair.TryGetValue((l.ItemId, l.LocationId), out var current)
                                       ? current.Sum(b => b.Quantity)
                                       : 0)))
            return Result.Fail(new StockErrors.BulkConcurrencyConflict(request.ClassId));

        // One StockAdjustedEvent per touched location refreshes the list and summary over SignalR.
        var eventLocations = new HashSet<Guid>();
        foreach (var line in request.Lines)
        {
            byPair.TryGetValue((line.ItemId, line.LocationId), out var lineBatches);

            var anchor = StockAdjuster.Apply(
                dbContext, lineBatches ?? [], request.ClassId, line.ItemId, line.LocationId,
                line.ActualQuantity, request.Reason, identityId);

            if (anchor is not null && eventLocations.Add(line.LocationId))
                anchor.AddDomainEvent(new StockAdjustedEvent(request.ClassId, line.LocationId));
        }

        // Single SaveChangesAsync: all lines persist or none do. The StockBatch rowversion turns a
        // concurrent change into DbUpdateConcurrencyException instead of a lost update.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail(new StockErrors.BulkConcurrencyConflict(request.ClassId));
        }

        return Result.Ok();
    }
}
