using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;
using skestock.Application.Features.Statistics.Queries.PurchaseSegments;
using skestock.Domain.Enums;

namespace skestock.Application.Features.OrderLists.Queries.GetOrderCatalog;

public class GetOrderCatalogQueryHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<GetOrderCatalogQuery, Result<List<OrderCatalogItemDto>>>
{
    public async ValueTask<Result<List<OrderCatalogItemDto>>> Handle(
        GetOrderCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var classId = request.ClassId;

        var classExists = await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == classId, cancellationToken);
        if (!classExists)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(classId));

        var items = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Sku,
                i.ImageFileId,
                i.Unit,
                i.LocationId,
                LocationName = i.Location == null ? null : i.Location.Name,
                LastQuantity = dbContext.ItemPurchaseStatistics
                    .Where(s => s.ItemId == i.Id && s.Scope == PurchaseStatisticsScope.Last365Days)
                    .Select(s => s.LastQuantity)
                    .FirstOrDefault(),
                LastPurchasedAt = dbContext.ItemPurchaseStatistics
                    .Where(s => s.ItemId == i.Id && s.Scope == PurchaseStatisticsScope.Last365Days)
                    .Select(s => (DateTimeOffset?)s.LastPurchasedAt)
                    .FirstOrDefault(),
                PurchaseCount = dbContext.ItemPurchaseStatistics
                    .Where(s => s.ItemId == i.Id && s.Scope == PurchaseStatisticsScope.Last365Days)
                    .Select(s => s.PurchaseCount)
                    .FirstOrDefault(),
                AverageIntervalDays = dbContext.ItemPurchaseStatistics
                    .Where(s => s.ItemId == i.Id && s.Scope == PurchaseStatisticsScope.Last365Days)
                    .Select(s => s.AverageIntervalDays)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var stock = await dbContext.StockBatches
            .AsNoTracking()
            .Where(b => b.ReceivedClassId == classId && b.Item.IsActive)
            .GroupBy(b => new { b.ItemId, b.LocationId, LocationName = b.Location.Name })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.LocationId,
                g.Key.LocationName,
                Quantity = g.Sum(b => b.Quantity)
            })
            .ToListAsync(cancellationToken);

        var thresholds = await dbContext.ItemLocationThresholds
            .AsNoTracking()
            .Where(t => dbContext.StockBatches.Any(b =>
                b.ReceivedClassId == classId && b.ItemId == t.ItemId && b.LocationId == t.LocationId))
            .Select(t => new { t.ItemId, t.LocationId, t.MinThreshold })
            .ToDictionaryAsync(t => (t.ItemId, t.LocationId), t => t.MinThreshold, cancellationToken);

        var hidden = (await dbContext.ClassItemStockVisibilities
                .AsNoTracking()
                .Where(v => v.ClassId == classId && v.HideWhenZeroStock)
                .Select(v => new { v.ItemId, v.LocationId })
                .ToListAsync(cancellationToken))
            .Select(v => (v.ItemId, v.LocationId))
            .ToHashSet();

        var segments = PurchaseSegmentClassifier.Classify(
            await PurchaseSegmentCandidates.LoadAsync(dbContext, cancellationToken),
            timeProvider.GetUtcNow());
        var frequentIds = segments.Weekly.Concat(segments.EveryTwoWeeks).Select(i => i.ItemId).ToHashSet();

        // Same visibility rule as the stock report: a zero-stock row hidden for this class is dropped.
        var locationsByItem = stock
            .Where(s => !(hidden.Contains((s.ItemId, s.LocationId)) && s.Quantity <= 0))
            .ToLookup(s => s.ItemId);

        var result = items
            .Select(i =>
            {
                var locations = locationsByItem[i.Id]
                    .Select(s => new OrderCatalogLocationDto
                    {
                        LocationId = s.LocationId,
                        LocationName = s.LocationName,
                        Quantity = s.Quantity,
                        // A missing threshold row means never low.
                        IsLowStock = thresholds.TryGetValue((i.Id, s.LocationId), out var min) && s.Quantity < min
                    })
                    .OrderBy(l => l.LocationName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (locations.Count == 0 && i.LocationId is { } defaultId && i.LocationName is { } defaultName)
                    locations.Add(new OrderCatalogLocationDto { LocationId = defaultId, LocationName = defaultName });

                return new OrderCatalogItemDto
                {
                    ItemId = i.Id,
                    Name = i.Name,
                    Sku = i.Sku,
                    ImageFileId = i.ImageFileId,
                    Unit = i.Unit,
                    LastQuantity = i.LastQuantity,
                    PurchaseCount = i.PurchaseCount,
                    LastPurchasedAt = i.LastPurchasedAt,
                    AverageIntervalDays = i.AverageIntervalDays,
                    IsFrequent = frequentIds.Contains(i.Id),
                    Locations = locations
                };
            })
            .ToList();

        return Result.Ok(result);
    }
}
