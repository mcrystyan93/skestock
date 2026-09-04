using skestock.Application.Common.Interfaces;
using skestock.Domain.Events.Categories;

namespace skestock.Application.Features.Categories.EventHandlers;

public class CategoryUpdatedEventHandler(IRealtimeNotifier notifier) : INotificationHandler<CategoryUpdatedEvent>
{
    public async ValueTask Handle(CategoryUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var payload = new { CategoryId = notification.Category.Id };

        await notifier.NotifyGroupAsync("categories-list", "CategoryUpdated", payload, cancellationToken);
    }
}
