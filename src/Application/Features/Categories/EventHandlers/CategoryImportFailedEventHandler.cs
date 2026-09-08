using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Categories;

namespace skestock.Application.Features.Categories.EventHandlers;

public class CategoryImportFailedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<CategoryImportFailedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.CategoryImportListTag
    ];

    public async ValueTask Handle(CategoryImportFailedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { CategoryImportId = notification.ImportId };

        await notifier.NotifyGroupAsync(
            RealtimeGroups.CategoryImportsList,
            RealtimeEvents.CategoryImportProcessed,
            payload,
            cancellationToken);
    }
}
