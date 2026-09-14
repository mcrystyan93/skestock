using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Categories;

namespace skestock.Application.Features.Categories.EventHandlers;

/// <summary>Publishes category import batch processing updates.</summary>
public class CategoryImportBatchCompletedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<CategoryImportBatchCompletedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.CategoryImportBatchListTag
    ];

    public async ValueTask Handle(CategoryImportBatchCompletedEvent notification, CancellationToken cancellationToken)
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
