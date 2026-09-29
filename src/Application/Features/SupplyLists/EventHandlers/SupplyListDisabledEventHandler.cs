using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.SupplyLists;

namespace skestock.Application.Features.SupplyLists.EventHandlers;

public class SupplyListDisabledEventHandler(IRealtimeNotifier notifier) : INotificationHandler<SupplyListDisabledEvent>
{
    public async ValueTask Handle(SupplyListDisabledEvent notification, CancellationToken cancellationToken) =>
        await notifier.NotifyGroupAsync(
            RealtimeGroups.SupplyListsList,
            RealtimeEvents.SupplyListDisabled,
            new { SupplyListId = notification.SupplyList.Id },
            cancellationToken);
}
