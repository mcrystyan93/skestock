using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.SupplyLists;

namespace skestock.Application.Features.SupplyLists.EventHandlers;

public class SupplyListCreatedEventHandler(IRealtimeNotifier notifier) : INotificationHandler<SupplyListCreatedEvent>
{
    public async ValueTask Handle(SupplyListCreatedEvent notification, CancellationToken cancellationToken) =>
        await notifier.NotifyGroupAsync(
            RealtimeGroups.SupplyListsList,
            RealtimeEvents.SupplyListCreated,
            new { SupplyListId = notification.SupplyList.Id },
            cancellationToken);
}
