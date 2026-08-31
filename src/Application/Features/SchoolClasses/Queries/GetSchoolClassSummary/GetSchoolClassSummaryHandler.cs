using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Models;

namespace skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassSummary;

public class GetSchoolClassSummaryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetSchoolClassSummaryQuery, Result<SchoolClassSummary>>
{
    public async ValueTask<Result<SchoolClassSummary>> Handle(GetSchoolClassSummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await dbContext.SchoolClasses
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new
            {
                c.Id,
                NoOfGoodsReceipt = c.GoodsReceipts.Count,
                TotalAmount = c.GoodsReceipts.Sum(r => (decimal?)r.TotalAmount) ?? 0m
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (summary is null)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.Id));

        // Low stock = sum of StockBatch.Quantity per item, across all locations for this class,
        // is less than the item's MinThreshold. Items with no batches for this class are excluded
        // (mirrors GetClassLocationStockHandler's "no batches -> doesn't appear" behaviour).
        var lowStockItemsCount = await dbContext.StockBatches
            .AsNoTracking()
            .Where(b => b.ReceivedClassId == request.Id)
            .GroupBy(b => b.ItemId)
            .Select(g => new { ItemId = g.Key, Quantity = g.Sum(b => b.Quantity) })
            .Join(dbContext.Items, s => s.ItemId, i => i.Id, (s, i) => new { s.Quantity, i.MinThreshold })
            .CountAsync(x => x.Quantity < x.MinThreshold, cancellationToken);

        return Result.Ok(new SchoolClassSummary(
            summary.Id,
            summary.NoOfGoodsReceipt,
            summary.TotalAmount,
            lowStockItemsCount));
    }
}
