using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Categories;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Categories.EventHandlers;

public class CategoryImportConfirmedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<CategoryImportConfirmedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.CategoryImportListTag,
        CacheConstants.CategoryListTag,
        StockCacheConstants.BuildCoarseTag()
    ];

    public async ValueTask Handle(CategoryImportConfirmedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { CategoryImportId = notification.ImportId };

        await notifier.NotifyGroupAsync(
            RealtimeGroups.CategoryImportsList,
            RealtimeEvents.CategoryImportConfirmed,
            payload,
            cancellationToken);
    }
}
