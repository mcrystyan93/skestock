using skestock.Application.Features.GoodsReceipts.Models;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptImportById;

public class GetGoodsReceiptImportByIdQuery : IRequest<Result<GoodsReceiptImportReviewDto>>
{
    public Guid Id { get; init; }
}
