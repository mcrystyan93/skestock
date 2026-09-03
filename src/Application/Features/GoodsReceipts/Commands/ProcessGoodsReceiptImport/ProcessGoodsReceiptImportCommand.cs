namespace skestock.Application.Features.GoodsReceipts.Commands.ProcessGoodsReceiptImport;

public class ProcessGoodsReceiptImportCommand(Guid goodsReceiptImportId) : IRequest<Result>
{
    public Guid GoodsReceiptImportId { get; init; } = goodsReceiptImportId;
}
