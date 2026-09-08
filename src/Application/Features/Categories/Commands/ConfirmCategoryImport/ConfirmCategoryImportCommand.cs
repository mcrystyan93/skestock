using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Categories.Models;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Categories.Commands.ConfirmCategoryImport;

// Requires an authenticated user: confirming creates Category rows whose CreatedBy audit column is
// stamped from the caller, mirroring the other authoring commands.
[Authorize]
public class ConfirmCategoryImportCommand : IRequest<Result<CategoryImportConfirmationResultDto>>, ICacheInvalidation
{
    public Guid ImportId { get; init; }

    // The reviewed list of category names. A future UI may edit or omit the AI's suggestions, so this
    // is the source of truth for what gets created - the stored extraction is only a hint.
    public List<string> CategoryNames { get; init; } = [];

    // New categories may be created, so invalidate the category list, the category-import list, and the
    // coarse stock report tag (current-stock reports can group by category).
    public IReadOnlyCollection<string> Tags =>
        [
            CacheConstants.CategoryListTag,
            CacheConstants.CategoryImportListTag,
            StockCacheConstants.BuildCoarseTag()
        ];
}
