using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Items;
using CategoryCacheConstants = skestock.Application.Features.Categories.CacheConstants;
using StockCacheConstants = skestock.Application.Features.Stock.CacheConstants;

namespace skestock.Application.Features.Items.EventHandlers;

public class ItemEnabledEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<ItemEnabledEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.ItemListTag,
        CategoryCacheConstants.CategoryListTag,
        StockCacheConstants.BuildCoarseTag()
    ];

    public async ValueTask Handle(ItemEnabledEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        await notifier.NotifyGroupAsync(
            RealtimeGroups.ItemsList,
            RealtimeEvents.ItemEnabled,
            new { ItemId = notification.Item.Id },
            cancellationToken);
    }
}
