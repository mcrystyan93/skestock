using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.SupplyLists;

namespace skestock.Application.Features.SupplyLists.EventHandlers;

public class SupplyListUpdatedEventHandler(IRealtimeNotifier notifier) : INotificationHandler<SupplyListUpdatedEvent>
{
    public async ValueTask Handle(SupplyListUpdatedEvent notification, CancellationToken cancellationToken) =>
        await notifier.NotifyGroupAsync(
            RealtimeGroups.SupplyListsList,
            RealtimeEvents.SupplyListUpdated,
            new { SupplyListId = notification.SupplyList.Id },
            cancellationToken);
}
