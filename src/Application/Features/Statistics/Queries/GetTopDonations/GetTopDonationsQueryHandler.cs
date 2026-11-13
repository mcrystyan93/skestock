using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetTopDonations;

public class GetTopDonationsQueryHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<GetTopDonationsQuery, Result<TopPurchasesDto>>
{
    public async ValueTask<Result<TopPurchasesDto>> Handle(
        GetTopDonationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StockTransactions
            .AsNoTracking()
            .Where(t => t.Type == StockTransactionType.Donation && t.QuantityChange > 0);

        switch (request.Scope)
        {
            case PurchaseStatisticsScope.Class:
                query = query.Where(t => t.ClassId == request.ClassId);
                break;
            case PurchaseStatisticsScope.Last90Days:
                query = query.Where(t => t.CreatedAt >= timeProvider.GetUtcNow().AddDays(-90));
                break;
            default:
                query = query.Where(t => t.CreatedAt >= timeProvider.GetUtcNow().AddDays(-365));
                break;
        }

        if (request.CategoryId is { } categoryId)
        {
            query = query.Where(t => t.Item.CategoryId == categoryId);
        }

        // One row per donated item, so materializing before ranking stays small.
        var totals = await query
            .GroupBy(t => t.ItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                TotalQuantity = g.Sum(t => t.QuantityChange),
                TotalValue = g.Sum(t => t.QuantityChange * (t.Batch != null ? t.Batch.UnitPrice : 0m)),
                Count = g.Count(),
                Last = g.Max(t => t.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        var itemIds = totals.Select(t => t.ItemId).ToList();
        var items = await dbContext.Items
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.Sku, i.Unit, CategoryName = i.Category.Name })
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var all = totals.Select(t =>
        {
            var item = items[t.ItemId];
            return new PurchaseStatisticDto
            {
                ItemId = t.ItemId,
                ItemName = item.Name,
                Sku = item.Sku,
                Unit = item.Unit,
                CategoryName = item.CategoryName,
                TotalQuantity = t.TotalQuantity,
                TotalValue = Math.Round(t.TotalValue, 2),
                PurchaseCount = t.Count,
                AverageQuantity = Math.Round((decimal)t.TotalQuantity / t.Count, 2),
                AverageUnitPrice = t.TotalQuantity == 0 ? 0m : Math.Round(t.TotalValue / t.TotalQuantity, 2),
                LastPurchasedAt = t.Last
            };
        }).ToList();

        return Result.Ok(new TopPurchasesDto
        {
            ComputedAt = timeProvider.GetUtcNow(),
            ByQuantity = all.OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.ItemName).Take(request.Top).ToList(),
            ByValue = all.OrderByDescending(s => s.TotalValue).ThenBy(s => s.ItemName).Take(request.Top).ToList(),
            ByFrequency = all.OrderByDescending(s => s.PurchaseCount).ThenByDescending(s => s.TotalQuantity)
                .ThenBy(s => s.ItemName).Take(request.Top).ToList()
        });
    }
}
