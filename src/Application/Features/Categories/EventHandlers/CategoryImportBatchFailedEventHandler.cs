using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Categories;

namespace skestock.Application.Features.Categories.EventHandlers;

/// <summary>Publishes category import batch failure updates.</summary>
public class CategoryImportBatchFailedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<CategoryImportBatchFailedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.CategoryImportBatchListTag
    ];

    public async ValueTask Handle(CategoryImportBatchFailedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { CategoryImportBatchId = notification.BatchId };

        await notifier.NotifyGroupAsync(
            RealtimeGroups.CategoryImportBatchesList,
            RealtimeEvents.CategoryImportBatchProcessed,
            payload,
            cancellationToken);
    }
}
