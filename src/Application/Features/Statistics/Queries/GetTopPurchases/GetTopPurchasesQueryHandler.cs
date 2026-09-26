using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetTopPurchases;

public class GetTopPurchasesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetTopPurchasesQuery, Result<TopPurchasesDto>>
{
    public async ValueTask<Result<TopPurchasesDto>> Handle(
        GetTopPurchasesQuery request,
        CancellationToken cancellationToken)
    {
        var classId = request.Scope == PurchaseStatisticsScope.Class ? request.ClassId : null;

        var query = dbContext.ItemPurchaseStatistics
            .AsNoTracking()
            .Where(s => s.Scope == request.Scope && s.ClassId == classId);

        if (request.CategoryId is { } categoryId)
        {
            query = query.Where(s => s.Item.CategoryId == categoryId);
        }

        var projected = query.Select(PurchaseStatisticProjection.ToDto);

        var byQuantity = await projected
            .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.ItemName)
            .Take(request.Top)
            .ToListAsync(cancellationToken);

        var byValue = await projected
            .OrderByDescending(s => s.TotalValue).ThenBy(s => s.ItemName)
            .Take(request.Top)
            .ToListAsync(cancellationToken);

        var byFrequency = await projected
            .OrderByDescending(s => s.PurchaseCount).ThenByDescending(s => s.TotalQuantity).ThenBy(s => s.ItemName)
            .Take(request.Top)
            .ToListAsync(cancellationToken);

        var computedAt = await query
            .Select(s => (DateTimeOffset?)s.ComputedAt)
            .MaxAsync(cancellationToken);

        return Result.Ok(new TopPurchasesDto
        {
            ComputedAt = computedAt,
            ByQuantity = byQuantity,
            ByValue = byValue,
            ByFrequency = byFrequency
        });
    }
}
