using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Security;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;

public class ConfirmGoodsReceiptImportCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<ConfirmGoodsReceiptImportCommand, Result<GoodsReceiptDto>>
{
    public async ValueTask<Result<GoodsReceiptDto>> Handle(ConfirmGoodsReceiptImportCommand request,
        CancellationToken cancellationToken)
    {
        // AuthorizationBehaviour guarantees an authenticated caller. GoodsReceiptImport/StockTransaction
        // FKs point at UserProfile.IdentityId, so the raw identity id is stored directly (mirrors
        // CreateGoodsReceiptCommandHandler).
        var identityId = Guard.Against.Null(user.Id, message: "Confirming a goods receipt import requires an authenticated user.");

        var import = await dbContext.GoodsReceiptImports
            .FirstOrDefaultAsync(i => i.Id == request.ImportId, cancellationToken);

        if (import is null)
            return Result.Fail(new GoodsReceiptImportErrors.GoodsReceiptImportNotFound(request.ImportId));

        // Idempotency for an accidental double-submit: if it's already confirmed, return the receipt
        // that was produced rather than creating a second one.
        if (import.Status == GoodsReceiptImportStatus.Confirmed && import.ResultingGoodsReceiptId is { } existingId)
            return await ProjectReceiptAsync(existingId, cancellationToken);

        if (import.Status != GoodsReceiptImportStatus.PendingReview)
            return Result.Fail(new GoodsReceiptImportErrors.GoodsReceiptImportNotInReview(request.ImportId, import.Status));

        // Expose the resolved class id so ICacheInvalidation.Tags (read after this handler) can build
        // the per-(class, location) stock tags.
        request.ClassId = import.ClassId;

        var newItemsByKey = await ResolveNewItemsAsync(request.Lines, cancellationToken);

        // Load perishable flag + shelf life for existing referenced items, so an existing-item line
        // that omits an expiry date can have it derived (ReceivedDate + ShelfLifeDays). New-item
        // lines have no persisted shelf life yet, so their expiry stays as supplied.
        var existingItemIds = request.Lines
            .Where(l => l.ItemId is not null)
            .Select(l => l.ItemId!.Value)
            .Distinct()
            .ToList();
        var itemInfoById = await dbContext.Items
            .AsNoTracking()
            .Where(i => existingItemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.IsPerishable, i.ShelfLifeDays })
            .ToDictionaryAsync(i => i.Id, i => (i.IsPerishable, i.ShelfLifeDays), cancellationToken);

        var receipt = new GoodsReceipt
        {
            ClassId = import.ClassId,
            Class = null!,
            SupplierReference = string.IsNullOrWhiteSpace(request.SupplierReference) ? null : request.SupplierReference.Trim(),
            Note = request.Note.Trim(),
        };

        var receivedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var line in request.Lines)
        {
            var newItem = line.ItemId is null ? newItemsByKey[BuildNewItemKey(line)] : null;

            var expiryDate = line.ExpiryDate;
            if (expiryDate is null
                && line.ItemId is { } lineItemId
                && itemInfoById.TryGetValue(lineItemId, out var info)
                && info.IsPerishable
                && info.ShelfLifeDays is > 0)
            {
                expiryDate = receivedDate.AddDays(info.ShelfLifeDays.Value);
            }

            var batch = new StockBatch
            {
                ItemId = line.ItemId ?? Guid.Empty,
                Item = newItem!,
                LocationId = line.LocationId,
                ReceivedClassId = import.ClassId,
                Quantity = line.Quantity,
                ExpiryDate = expiryDate,
                ReceivedDate = receivedDate,
                UnitPrice = line.UnitPrice,
                GoodsReceipt = receipt
            };

            var transaction = new StockTransaction
            {
                ItemId = line.ItemId ?? Guid.Empty,
                Item = newItem!,
                LocationId = line.LocationId,
                Batch = batch,
                ClassId = import.ClassId,
                UserId = identityId,
                Type = StockTransactionType.Order,
                QuantityChange = line.Quantity,
                GoodsReceipt = receipt
            };

            receipt.Batches.Add(batch);
            receipt.Transactions.Add(transaction);
            receipt.TotalAmount += batch.LineTotal;
        }

        dbContext.GoodsReceipts.Add(receipt);

        import.MarkAsConfirmed(receipt.Id);

        // Single SaveChanges wraps the receipt header, every batch/transaction, and any newly created
        // items/categories in one implicit transaction: all-or-nothing.
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectReceiptAsync(receipt.Id, cancellationToken);
    }

    /// <summary>
    /// For every line that creates a new item (no ItemId), resolves its category (matching an existing
    /// one by name, otherwise creating it) and builds the new <see cref="Item"/>. Both items and
    /// categories are de-duplicated within the request so repeated/split lines share one instance.
    /// The entities are attached (via <c>Add</c>) but only persisted by the caller's SaveChanges.
    /// </summary>
    private async Task<Dictionary<string, Item>> ResolveNewItemsAsync(
        List<ConfirmGoodsReceiptImportLine> lines, CancellationToken cancellationToken)
    {
        var newItemLines = lines.Where(l => l.ItemId is null).ToList();
        var result = new Dictionary<string, Item>();

        if (newItemLines.Count == 0)
            return result;

        var categoriesByName = await ResolveCategoriesAsync(newItemLines, cancellationToken);

        foreach (var line in newItemLines)
        {
            var key = BuildNewItemKey(line);
            if (result.ContainsKey(key))
                continue;

            var category = categoriesByName[line.CategoryName!.Trim().ToLowerInvariant()];

            var item = new Item
            {
                Sku = string.IsNullOrWhiteSpace(line.Sku) ? null : line.Sku.Trim(),
                Name = line.Name!.Trim(),
                Unit = string.IsNullOrWhiteSpace(line.Unit) ? "unit" : line.Unit.Trim(),
                IsPerishable = line.IsPerishable,
                IsActive = true,
                Category = category,
                CategoryId = category.Id
            };

            dbContext.Items.Add(item);
            result[key] = item;
        }

        return result;
    }

    private async Task<Dictionary<string, Category>> ResolveCategoriesAsync(
        List<ConfirmGoodsReceiptImportLine> newItemLines, CancellationToken cancellationToken)
    {
        var names = newItemLines
            .Select(l => l.CategoryName!.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var upperNames = names.Select(n => n.ToUpper()).ToList();

        // Compare on ToUpper() on both sides (translatable to SQL) so category matching is
        // case-insensitive regardless of the extracted category name's casing.
        var existing = await dbContext.Categories
            .Where(c => upperNames.Contains(c.Name.ToUpper()))
            .ToListAsync(cancellationToken);

        var byName = existing.ToDictionary(c => c.Name.Trim().ToLowerInvariant(), c => c);

        foreach (var name in names)
        {
            var key = name.ToLowerInvariant();
            if (byName.ContainsKey(key))
                continue;

            var category = new Category { Name = name };
            dbContext.Categories.Add(category);
            byName[key] = category;
        }

        return byName;
    }

    // De-dup key for a new item within one request: SKU when present (globally unique intent),
    // otherwise name + category so two different categories don't collapse into one item.
    private static string BuildNewItemKey(ConfirmGoodsReceiptImportLine line) =>
        !string.IsNullOrWhiteSpace(line.Sku)
            ? $"sku:{line.Sku.Trim().ToLowerInvariant()}"
            : $"name:{line.Name?.Trim().ToLowerInvariant()}|cat:{line.CategoryName?.Trim().ToLowerInvariant()}";

    private async Task<Result<GoodsReceiptDto>> ProjectReceiptAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var dto = await dbContext.GoodsReceipts
            .AsNoTracking()
            .Where(r => r.Id == receiptId)
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
