using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.SupplyLists;

namespace skestock.Application.Features.SupplyLists.EventHandlers;

public class SupplyListEnabledEventHandler(IRealtimeNotifier notifier) : INotificationHandler<SupplyListEnabledEvent>
{
    public async ValueTask Handle(SupplyListEnabledEvent notification, CancellationToken cancellationToken) =>
        await notifier.NotifyGroupAsync(
            RealtimeGroups.SupplyListsList,
            RealtimeEvents.SupplyListEnabled,
            new { SupplyListId = notification.SupplyList.Id },
            cancellationToken);
}
