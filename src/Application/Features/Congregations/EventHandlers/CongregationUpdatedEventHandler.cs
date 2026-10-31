using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Congregations;

namespace skestock.Application.Features.Congregations.EventHandlers;

public class CongregationUpdatedEventHandler(IRealtimeNotifier notifier) : INotificationHandler<CongregationUpdatedEvent>
{
    public async ValueTask Handle(CongregationUpdatedEvent notification, CancellationToken cancellationToken) =>
        await notifier.NotifyGroupAsync(
            RealtimeGroups.CongregationsList,
            RealtimeEvents.CongregationUpdated,
            new { CongregationId = notification.Congregation.Id },
            cancellationToken);
}
