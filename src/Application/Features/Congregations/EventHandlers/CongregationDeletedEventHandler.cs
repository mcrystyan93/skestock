using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Congregations;

namespace skestock.Application.Features.Congregations.EventHandlers;

public class CongregationDeletedEventHandler(IRealtimeNotifier notifier) : INotificationHandler<CongregationDeletedEvent>
{
    public async ValueTask Handle(CongregationDeletedEvent notification, CancellationToken cancellationToken) =>
        await notifier.NotifyGroupAsync(
            RealtimeGroups.CongregationsList,
            RealtimeEvents.CongregationDeleted,
            new { CongregationId = notification.Congregation.Id },
            cancellationToken);
}
