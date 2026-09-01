using skestock.Application.Common.Interfaces;
using skestock.Application.Features.StockBatches.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.StockBatches.Commands.CreateStockBatch;

public class CreateStockBatchCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CreateStockBatchCommand, Result<StockBatchListItemDto>>
{
    public async ValueTask<Result<StockBatchListItemDto>> Handle(CreateStockBatchCommand request, CancellationToken cancellationToken)
    {
        // AuthorizationBehaviour (earlier in the pipeline) guarantees an authenticated caller, so
        // IUser.Id is expected to be populated. StockTransaction.UserId is a FK to
        // UserProfile.IdentityId (not UserProfile.Id), so the raw identity id can be stored
        // directly - same rationale as CreateGoodsReceiptCommandHandler/AdjustStockCommandHandler.
        var identityId = Guard.Against.Null(user.Id, message: "Creating a stock batch requires an authenticated user.");

        // GoodsReceiptId is always null here: this is explicitly the "outside a receipt"
        // creation path (see StockBatch.GoodsReceiptId doc comment, e.g. manual entry/rollover).
        var batch = new StockBatch
        {
            ItemId = request.ItemId,
            LocationId = request.LocationId,
            ReceivedClassId = request.ReceivedClassId,
            Quantity = request.Quantity,
            ExpiryDate = request.ExpiryDate,
            ReceivedDate = request.ReceivedDate,
            UnitPrice = request.UnitPrice,
            GoodsReceiptId = null
        };

        var transaction = new StockTransaction
        {
            ItemId = request.ItemId,
            LocationId = request.LocationId,
            Batch = batch,
            ClassId = request.ReceivedClassId,
            UserId = identityId,
            Type = StockTransactionType.Order,
            QuantityChange = request.Quantity,
            GoodsReceiptId = null
        };

        dbContext.StockBatches.Add(batch);
        dbContext.StockTransactions.Add(transaction);

        // A single SaveChangesAsync wraps both inserts in one implicit DB transaction - if
        // either fails, nothing partially saves.
        await dbContext.SaveChangesAsync(cancellationToken);

        // Navigations aren't loaded on freshly-inserted entities (only *Id FKs are set), so item/
        // location names are resolved with a follow-up projection - mirrors
        // CreateGoodsReceiptCommandHandler's follow-up projection, reusing the same
        // StockBatchListItemDto shape GetAllStockBatchesHandler projects to.
        var dto = await dbContext.StockBatches
            .AsNoTracking()
            .Where(b => b.Id == batch.Id)
            .Select(b => new StockBatchListItemDto
            {
                Id = b.Id,
                ItemId = b.ItemId,
                ItemName = b.Item.Name,
                LocationId = b.LocationId,
                LocationName = b.Location.Name,
                GoodsReceiptId = b.GoodsReceiptId,
                Quantity = b.Quantity,
                UnitPrice = b.UnitPrice,
                LineTotal = b.Quantity * b.UnitPrice,
                ExpiryDate = b.ExpiryDate,
                ReceivedDate = b.ReceivedDate,
                CreatedDate = b.CreatedDate
            })
            .SingleAsync(cancellationToken);

        return Result.Ok(dto);
    }
}
