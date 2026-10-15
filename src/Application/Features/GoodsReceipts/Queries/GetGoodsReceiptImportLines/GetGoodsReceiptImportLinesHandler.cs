using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptImportLines;

public class GetGoodsReceiptImportLinesHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetGoodsReceiptImportLinesQuery, Result<List<GoodsReceiptImportLineDto>>>
{
    public async ValueTask<Result<List<GoodsReceiptImportLineDto>>> Handle(
        GetGoodsReceiptImportLinesQuery query, CancellationToken cancellationToken)
    {
        if (!await dbContext.GoodsReceipts.AsNoTracking()
                .AnyAsync(r => r.Id == query.Id, cancellationToken))
            return Result.Fail(new GoodsReceiptErrors.GoodsReceiptNotFound(query.Id));

        var lines = await dbContext.GoodsReceiptImportLines
            .AsNoTracking()
            .Where(l => l.Import.ResultingGoodsReceiptId == query.Id
                        && l.Import.Status == GoodsReceiptImportStatus.Confirmed)
            .OrderBy(l => l.CreatedDate)
            .ThenBy(l => l.Id)
            .Select(l => new GoodsReceiptImportLineDto
            {
                Id = l.Id,
                ImportId = l.ImportId,
                RawItemText = l.RawItemText,
                MatchedItemId = l.MatchedItemId,
                LocationId = l.LocationId,
                LocationName = l.Location != null ? l.Location.Name : null,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                ExpiryDate = l.ExpiryDate,
                LineTotal = l.Quantity * l.UnitPrice
            })
            .ToListAsync(cancellationToken);

        return Result.Ok(lines);
    }
}
