using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetPurchaseSegments;

public class GetPurchaseSegmentsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetPurchaseSegmentsQuery, Result<PurchaseSegmentsDto>>
{
    public async ValueTask<Result<PurchaseSegmentsDto>> Handle(
        GetPurchaseSegmentsQuery request,
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
                s.AverageIntervalDays
            })
            .ToListAsync(cancellationToken);

        var candidates = rows
            .GroupBy(r => r.ItemId)
            .Select(group =>
            {
                var year = group.FirstOrDefault(r => r.Scope == PurchaseStatisticsScope.Last365Days);
                var classRows = group.Where(r => r.Scope == PurchaseStatisticsScope.Class).ToList();
                var first = group.First();

                // OncePerClass items may have no Last365Days row: fall back to the per-class averages.
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

                return new PurchaseSegmentCandidate(item, classRows.Select(r => r.PurchaseCount).ToList());
            });

        return Result.Ok(PurchaseSegmentClassifier.Classify(candidates));
    }
}
