using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Categories;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Categories.EventHandlers;

/// <summary>Publishes category import batch confirmation updates.</summary>
public class CategoryImportBatchConfirmedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<CategoryImportBatchConfirmedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.CategoryImportBatchListTag,
        CacheConstants.CategoryListTag,
        StockCacheConstants.BuildCoarseTag()
    ];

    public async ValueTask Handle(CategoryImportBatchConfirmedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { CategoryImportBatchId = notification.BatchId };

        await notifier.NotifyGroupAsync(
            RealtimeGroups.CategoryImportBatchesList,
            RealtimeEvents.CategoryImportBatchConfirmed,
            payload,
            cancellationToken);
    }
}
