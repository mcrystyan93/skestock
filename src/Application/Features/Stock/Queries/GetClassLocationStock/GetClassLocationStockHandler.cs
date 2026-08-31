using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Stock.Models;

namespace skestock.Application.Features.Stock.Queries.GetClassLocationStock;

public class GetClassLocationStockHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetClassLocationStockQuery, Result<List<StockItemDto>>>
{
    public async ValueTask<Result<List<StockItemDto>>> Handle(GetClassLocationStockQuery request,
        CancellationToken cancellationToken)
    {
        var classExists = await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.ClassId, cancellationToken);
        if (!classExists)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.ClassId));

        var location = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.Id == request.LocationId)
            .Select(l => new { l.Id, l.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (location is null)
            return Result.Fail(new LocationErrors.LocationNotFound(request.LocationId));

        // Current stock for an item = sum of the remaining Quantity across every StockBatch
        // received by this class at this location. Items with no batches here simply don't
        // appear (there's nothing to sum), matching the "sum of stockBatches" definition literally.
        // Grouping and the Item lookup are done as two separate queries rather than reading
        // Item off the group (e.g. g.First().Item) - that pattern doesn't translate to SQL.
        var stockByItem = await dbContext.StockBatches
            .AsNoTracking()
            .Where(b => b.ReceivedClassId == request.ClassId && b.LocationId == request.LocationId)
            .GroupBy(b => b.ItemId)
            .Select(g => new { ItemId = g.Key, Quantity = g.Sum(b => b.Quantity) })
            .ToListAsync(cancellationToken);

        if (stockByItem.Count == 0)
            return Result.Ok(new List<StockItemDto>());

        var itemIds = stockByItem.Select(x => x.ItemId).ToList();
        var items = await dbContext.Items
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.Unit, i.IsPerishable, i.MinThreshold })
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var data = stockByItem
            .Select(x =>
            {
                var item = items[x.ItemId];
                return new StockItemDto
                {
                    ItemId = x.ItemId,
                    ItemName = item.Name,
                    LocationId = location.Id,
                    LocationName = location.Name,
                    Unit = item.Unit,
                    IsPerishable = item.IsPerishable,
                    Quantity = x.Quantity,
                    IsLowStock = x.Quantity < item.MinThreshold
                };
            })
            .OrderBy(x => x.ItemName)
            .ToList();

        return Result.Ok(data);
    }
}
