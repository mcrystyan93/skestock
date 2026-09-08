using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Categories;

namespace skestock.Application.Features.Categories.EventHandlers;

public class CategoryImportCreatedEventHandler(
    IRealtimeNotifier notifier,
    HybridCache cache)
    : INotificationHandler<CategoryImportCreatedEvent>
{
    private readonly IReadOnlyCollection<string> _tags =
    [
        CacheConstants.CategoryImportListTag
    ];

    public async ValueTask Handle(CategoryImportCreatedEvent notification, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { CategoryImportId = notification.Import.Id };
        await notifier.NotifyGroupAsync(
            RealtimeGroups.CategoryImportsList,
            RealtimeEvents.CategoryImportCreated,
            payload,
            cancellationToken);
    }
}
