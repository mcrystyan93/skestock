using Microsoft.Extensions.Caching.Hybrid;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Categories;

namespace skestock.Application.Features.Categories.EventHandlers;

public class CategoryUpdatedEventHandler(IRealtimeNotifier notifier, HybridCache cache)
    : INotificationHandler<CategoryUpdatedEvent>
{
    private readonly IReadOnlyCollection<string> _tags = [CacheConstants.CategoryListTag];

    public async ValueTask Handle(CategoryUpdatedEvent notification, CancellationToken cancellationToken)
    {
        // invalidate cache
        await cache.RemoveByTagAsync(_tags, cancellationToken);

        var payload = new { CategoryId = notification.Category.Id };

        await notifier.NotifyGroupAsync(
            RealtimeGroups.CategoriesList,
            RealtimeEvents.CategoryUpdated,
            payload,
            cancellationToken);
    }
}
