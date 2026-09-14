using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Categories;

namespace skestock.Application.Features.Categories.EventHandlers;

/// <summary>Publishes category import batch creation updates.</summary>
public class CategoryImportBatchCreatedEventHandler(
    IRealtimeNotifier notifier,
    HybridCache cache)
    : INotificationHandler<CategoryImportBatchCreatedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.CategoryImportBatchListTag
    ];

    public async ValueTask Handle(CategoryImportBatchCreatedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { CategoryImportBatchId = notification.Batch.Id };
        await notifier.NotifyGroupAsync(
            RealtimeGroups.CategoryImportBatchesList,
            RealtimeEvents.CategoryImportBatchCreated,
            payload,
            cancellationToken);
    }
}
