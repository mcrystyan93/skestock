using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetPurchaseSegments;

public static class PurchaseSegmentCandidates
{
    public static async Task<List<PurchaseSegmentCandidate>> LoadAsync(
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.ItemPurchaseStatistics
            .AsNoTracking()
            .Where(s => s.Item.IsActive
                        && (s.Scope == PurchaseStatisticsScope.Last365Days || s.Scope == PurchaseStatisticsScope.Class))
            .Select(s => new
            {
                s.Scope,
                s.ItemId,
                s.Item.Name,
                s.Item.Sku,
                s.Item.ImageFileId,
                s.Item.Unit,
                s.AverageQuantity,
                s.PurchaseCount,
                s.AverageIntervalDays,
                s.LastPurchasedAt
            })
            .ToListAsync(cancellationToken);

        var candidates = rows
            .GroupBy(r => r.ItemId)
            .Select(group =>
            {
                var year = group.FirstOrDefault(r => r.Scope == PurchaseStatisticsScope.Last365Days);
                var classRows = group.Where(r => r.Scope == PurchaseStatisticsScope.Class).ToList();
                var first = group.First();

                // Item fields (name, sku, ...) come from `first`; the figures come from `year`.
                // OncePerClass items may have no Last365Days row, so the figures fall back to per-class averages.
                var item = new PurchaseSegmentItemDto
                {
                    ItemId = first.ItemId,
                    Name = first.Name,
                    Sku = first.Sku,
                    ImageFileId = first.ImageFileId,
                    Unit = first.Unit,
                    AverageQuantity = year?.AverageQuantity
                                      ?? (classRows.Count == 0 ? 0m : Math.Round(classRows.Average(r => r.AverageQuantity), 2)),
                    PurchaseCount = year?.PurchaseCount
                                    ?? (classRows.Count == 0 ? 0 : (int)Math.Round(classRows.Average(r => r.PurchaseCount))),
                    AverageIntervalDays = year?.AverageIntervalDays
                };

                return new PurchaseSegmentCandidate(
                    item, year?.LastPurchasedAt, classRows.Select(r => r.PurchaseCount).ToList());
            })
            .ToList();

        return candidates;
    }
}
