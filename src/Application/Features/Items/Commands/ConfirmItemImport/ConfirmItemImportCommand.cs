using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Items.Models;
using CategoryCacheConstants = skestock.Application.Features.Categories.CacheConstants;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Items.Commands.ConfirmItemImport;

/// <summary>
/// One reviewed item to resolve on confirm. When <see cref="Sku"/> (case-insensitive, trimmed)
/// matches an existing item, that item is reused instead of creating a duplicate.
/// </summary>
public class ConfirmItemImportItem
{
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string Unit { get; init; } = "unit";
    public string? Description { get; init; }
    public bool IsPerishable { get; init; }
}

// Requires an authenticated user: confirming creates Category/Item rows whose CreatedBy audit column
// is stamped from the caller, mirroring ConfirmCategoryImportCommand.
[Authorize]
public class ConfirmItemImportCommand : IRequest<Result<ItemImportConfirmationResultDto>>, ICacheInvalidation
{
    public Guid ImportId { get; init; }

    // The reviewed list of items. A future UI may edit or omit the AI's suggestions, so this is the
    // source of truth for what gets created/reused - the stored extraction is only a hint.
    public List<ConfirmItemImportItem> Items { get; init; } = [];

    // New items and/or categories may be created, so invalidate the item list, the item-import list,
    // the category list, and the coarse stock report tag (current-stock reports can group by
    // category/item), mirroring ConfirmGoodsReceiptImportCommand.
    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.ItemImportListTag,
        CacheConstants.ItemListTag,
        CategoryCacheConstants.CategoryListTag,
        StockCacheConstants.BuildCoarseTag()
    ];
}
