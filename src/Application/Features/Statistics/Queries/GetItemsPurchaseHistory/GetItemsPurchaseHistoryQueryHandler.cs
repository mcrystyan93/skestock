using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetItemsPurchaseHistory;

public class GetItemsPurchaseHistoryQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetItemsPurchaseHistoryQuery, Result<List<PurchaseStatisticDto>>>
{
    public async ValueTask<Result<List<PurchaseStatisticDto>>> Handle(
        GetItemsPurchaseHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var itemIds = request.ItemIds.Distinct().ToList();
        if (itemIds.Count == 0)
        {
            return Result.Ok(new List<PurchaseStatisticDto>());
        }

        var history = await dbContext.ItemPurchaseStatistics
            .AsNoTracking()
            .Where(s => s.Scope == PurchaseStatisticsScope.Last365Days
                        && s.ClassId == null
                        && itemIds.Contains(s.ItemId))
            .Select(PurchaseStatisticProjection.ToDto)
            .ToListAsync(cancellationToken);

        return Result.Ok(history);
    }
}
