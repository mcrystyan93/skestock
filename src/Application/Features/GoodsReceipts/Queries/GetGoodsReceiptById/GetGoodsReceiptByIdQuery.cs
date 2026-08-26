using skestock.Application.Features.GoodsReceipts.Models;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptById;

public class GetGoodsReceiptByIdQuery : IRequest<Result<GoodsReceiptDto>>
{
    public int Id { get; init; }
}
