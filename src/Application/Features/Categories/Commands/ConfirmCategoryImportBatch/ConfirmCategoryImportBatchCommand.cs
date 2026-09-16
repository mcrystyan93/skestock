using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.Categories.Models;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Categories.Commands.ConfirmCategoryImportBatch;

[Authorize]
public class ConfirmCategoryImportBatchCommand : IRequest<Result<CategoryImportBatchConfirmationResponseDto>>, ICacheInvalidation
{
    public Guid BatchId { get; init; }
    public List<string> CategoryNames { get; init; } = [];

    public IReadOnlyCollection<string> Tags =>
    [
        CacheConstants.CategoryListTag,
        CacheConstants.CategoryImportBatchListTag,
        StockCacheConstants.BuildCoarseTag()
    ];
}
