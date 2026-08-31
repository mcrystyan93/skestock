using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Application.Features.GoodsReceipts.Models;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptById;

public class GetGoodsReceiptByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetGoodsReceiptByIdQuery, Result<GoodsReceiptDto>>
{
    public async ValueTask<Result<GoodsReceiptDto>> Handle(GetGoodsReceiptByIdQuery query,
        CancellationToken cancellationToken)
    {
        var receipt = await dbContext.GoodsReceipts
            .AsNoTracking()
            .Where(r => r.Id == query.Id)
            .Select(r => new GoodsReceiptDto
            {
                Id = r.Id,
                ClassId = r.ClassId,
                ClassName = r.Class.Name,
                ReceivedAt = r.ReceivedAt,
                SupplierReference = r.SupplierReference,
                Note = r.Note,
                TotalAmount = r.TotalAmount,
                // Lines = r.Batches.Select(b => new GoodsReceiptLineDto
                // {
                //     StockBatchId = b.Id,
                //     ItemId = b.ItemId,
                //     ItemName = b.Item.Name,
                //     LocationId = b.LocationId,
                //     LocationName = b.Location.Name,
                //     Quantity = b.Quantity,
                //     ExpiryDate = b.ExpiryDate
                // }).ToList(),
                CreatedByName = r.CreatedBy != null ? r.CreatedBy.FullName : null,
                LastModifiedByName = r.LastModifiedBy != null ? r.LastModifiedBy.FullName : null,
                CreatedDate = r.CreatedDate,
                LastModifiedDate = r.LastModifiedDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (receipt is null)
            return Result.Fail(new GoodsReceiptErrors.GoodsReceiptNotFound(query.Id));

        return Result.Ok(receipt);
    }
}
