using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Items.Models;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Items.Commands.ConfirmItemImport;

/// <summary>
/// One reviewed item to resolve on confirm. The selected item is required and must already exist.
/// The selected item is not mutated by confirmation.
/// </summary>
public class ConfirmItemImportItem
{
    public Guid ItemId { get; init; }
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = "unit";
    public string? Description { get; init; }
    public bool IsPerishable { get; init; }
}

// Requires an authenticated user because confirmation changes the import lifecycle and audit data.
[Authorize]
public class ConfirmItemImportCommand : IRequest<Result<ItemImportConfirmationResultDto>>, ICacheInvalidation
{
    public Guid ImportId { get; init; }

    // The reviewed list of items. A future UI may edit or omit the AI's suggestions, so this is the
    // source of truth for what gets associated - the stored extraction is only a hint.
    public List<ConfirmItemImportItem> Items { get; init; } = [];

    // Imported items affect item, import, and stock views, so invalidate all related collection tags
    // after confirmation.
    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.ItemImportListTag,
        CacheConstants.ItemListTag,
        StockCacheConstants.BuildCoarseTag()
    ];
}
