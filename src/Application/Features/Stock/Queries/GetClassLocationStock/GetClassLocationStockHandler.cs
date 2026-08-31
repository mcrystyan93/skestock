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

        if (request.LocationId is { } requestedLocationId)
        {
            var locationExists = await dbContext.Locations
                .AsNoTracking()
                .AnyAsync(l => l.Id == requestedLocationId, cancellationToken);
            if (!locationExists)
                return Result.Fail(new LocationErrors.LocationNotFound(requestedLocationId));
        }

        // Current stock for an item at a location = sum of the remaining Quantity across every
        // StockBatch received by this class at that location. Items/locations with no batches
        // simply don't appear, matching the "sum of stockBatches" definition literally.
        // When LocationId is omitted, group by (Item, Location) instead of just Item, so the
        // report still returns one row per item per location rather than collapsing quantities
        // from different locations into a single, ambiguous total.
        var batchesQuery = dbContext.StockBatches
            .AsNoTracking()
            .Where(b => b.ReceivedClassId == request.ClassId);

        if (request.LocationId is { } locationId)
            batchesQuery = batchesQuery.Where(b => b.LocationId == locationId);

        var stockByItemLocation = await batchesQuery
            .GroupBy(b => new { b.ItemId, b.LocationId })
            .Select(g => new { g.Key.ItemId, g.Key.LocationId, Quantity = g.Sum(b => b.Quantity) })
            .ToListAsync(cancellationToken);

        if (stockByItemLocation.Count == 0)
            return Result.Ok(new List<StockItemDto>());

        // Item and Location lookups are done as separate queries rather than reading them off the
        // group (e.g. g.First().Item) - that pattern doesn't translate to SQL.
        var itemIds = stockByItemLocation.Select(x => x.ItemId).Distinct().ToList();
        var itemsQuery = dbContext.Items
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            itemsQuery = itemsQuery.Where(i => i.Name.Contains(term));
        }

        var items = await itemsQuery
            .Select(i => new { i.Id, i.Name, i.Unit, i.IsPerishable, i.MinThreshold })
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        // Items excluded by the search term filter above must also be excluded from the stock
        // rows, since a StockBatch row only carries the ItemId, not the item's name.
        stockByItemLocation = stockByItemLocation.Where(x => items.ContainsKey(x.ItemId)).ToList();

        if (stockByItemLocation.Count == 0)
            return Result.Ok(new List<StockItemDto>());

        var locationIds = stockByItemLocation.Select(x => x.LocationId).Distinct().ToList();
        var locations = await dbContext.Locations
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .Select(l => new { l.Id, l.Name })
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var data = stockByItemLocation
            .Select(x =>
            {
                var item = items[x.ItemId];
                var location = locations[x.LocationId];
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
            .ThenBy(x => x.LocationName)
            .ToList();

        return Result.Ok(data);
    }
}
