using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Items.Models;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Items.Commands.ConfirmItemImportBatch;

/// <summary>
/// One reviewed item to resolve on confirm. The selected item is required and must already exist.
/// Carries the reviewed values alongside the selected catalog item for batch confirmation.
/// </summary>
public class ConfirmItemImportBatchItem
{
    public Guid ItemId { get; init; }
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = "unit";
    public string? Description { get; init; }
    public bool IsPerishable { get; init; }
}

// Requires an authenticated user because confirmation changes the batch lifecycle and audit data.
[Authorize]
public class ConfirmItemImportBatchCommand : IRequest<Result<ItemImportBatchConfirmationResultDto>>, ICacheInvalidation
{
    public Guid BatchId { get; init; }

    public List<ConfirmItemImportBatchItem> Items { get; init; } = [];

    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.ItemImportBatchListTag,
        CacheConstants.ItemListTag,
        StockCacheConstants.BuildCoarseTag()
    ];
}
