using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceipt;

public class CreateGoodsReceiptCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CreateGoodsReceiptCommand, Result<GoodsReceiptDto>>
{
    public async ValueTask<Result<GoodsReceiptDto>> Handle(CreateGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        // AuthorizationBehaviour (earlier in the pipeline) guarantees an authenticated caller for
        // any request handled here, so IUser.Id (the Identity/AspNetUsers id) is expected to be
        // populated. StockTransaction.UserId is a FK to UserProfile.IdentityId (not UserProfile.Id -
        // see StockTransactionConfiguration), so the raw identity id can be stored directly.
        var identityId = Guard.Against.Null(user.Id, message: "Creating a goods receipt requires an authenticated user.");

        var receipt = new GoodsReceipt
        {
            ClassId = request.ClassId,
            Class = null!,
            SupplierReference = string.IsNullOrWhiteSpace(request.SupplierReference) ? null : request.SupplierReference.Trim(),
            Note = request.Note.Trim()
        };

        var receivedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Load the perishable flag + shelf life for the referenced items so a line that omits an
        // expiry date can have it derived (ReceivedDate + ShelfLifeDays) for perishable items.
        var itemIds = request.Lines.Select(l => l.ItemId).Distinct().ToList();
        var itemInfoById = await dbContext.Items
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.IsPerishable, i.ShelfLifeDays })
            .ToDictionaryAsync(i => i.Id, i => (i.IsPerishable, i.ShelfLifeDays), cancellationToken);

        foreach (var line in request.Lines)
        {
            var expiryDate = line.ExpiryDate;
            if (expiryDate is null
                && itemInfoById.TryGetValue(line.ItemId, out var info)
                && info.IsPerishable
                && info.ShelfLifeDays is > 0)
            {
                expiryDate = receivedDate.AddDays(info.ShelfLifeDays.Value);
            }

            var batch = new StockBatch
            {
                ItemId = line.ItemId,
                LocationId = line.LocationId,
                ReceivedClassId = request.ClassId,
                Quantity = line.Quantity,
                ExpiryDate = expiryDate,
                ReceivedDate = receivedDate,
                UnitPrice = line.UnitPrice,
                GoodsReceipt = receipt
            };

            var transaction = new StockTransaction
            {
                ItemId = line.ItemId,
                LocationId = line.LocationId,
                Batch = batch,
                ClassId = request.ClassId,
                UserId = identityId,
                Type = StockTransactionType.Order,
                QuantityChange = line.Quantity,
                GoodsReceipt = receipt
            };

            receipt.Batches.Add(batch);
            receipt.Transactions.Add(transaction);
            // Captured at receipt time so later stock adjustments never retroactively change it.
            receipt.TotalAmount += batch.LineTotal;
        }

        dbContext.GoodsReceipts.Add(receipt);

        // A single SaveChangesAsync call wraps the header + every line's batch/transaction in one
        // implicit DB transaction: if any insert fails (e.g. a constraint violation), everything
        // rolls back together - nothing partially saved.
        await dbContext.SaveChangesAsync(cancellationToken);

        // Navigations aren't loaded on freshly-inserted entities (only *Id FKs are set), so item/
        // location/class names are resolved with a follow-up projection - mirrors
        // GetGoodsReceiptByIdHandler's projection (kept independent/duplicated rather than shared,
        // so each handler owns its own read shape).
        var dto = await dbContext.GoodsReceipts
            .AsNoTracking()
            .Where(r => r.Id == receipt.Id)
            .Select(r => new GoodsReceiptDto
            {
                Id = r.Id,
                ClassId = r.ClassId,
                ClassName = r.Class.Name,
                ReceivedAt = r.ReceivedAt,
                SupplierReference = r.SupplierReference,
                Note = r.Note,
                TotalAmount = r.TotalAmount,
                Lines = r.Batches.Select(b => new GoodsReceiptLineDto
                {
                    StockBatchId = b.Id,
                    ItemId = b.ItemId,
                    ItemName = b.Item.Name,
                    LocationId = b.LocationId,
                    LocationName = b.Location.Name,
                    Quantity = b.Quantity,
                    ExpiryDate = b.ExpiryDate,
                    UnitPrice = b.UnitPrice,
                    LineTotal = b.LineTotal
                }).ToList(),
                CreatedByName = r.CreatedBy != null ? r.CreatedBy.FullName : null,
                LastModifiedByName = r.LastModifiedBy != null ? r.LastModifiedBy.FullName : null,
                CreatedDate = r.CreatedDate,
                LastModifiedDate = r.LastModifiedDate
            })
            .SingleAsync(cancellationToken);

        return Result.Ok(dto);
    }
}

