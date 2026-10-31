using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Domain.Events.Congregations;

namespace skestock.Application.Features.Congregations.EventHandlers;

public class CongregationCreatedEventHandler(IRealtimeNotifier notifier) : INotificationHandler<CongregationCreatedEvent>
{
    public async ValueTask Handle(CongregationCreatedEvent notification, CancellationToken cancellationToken) =>
        await notifier.NotifyGroupAsync(
            RealtimeGroups.CongregationsList,
            RealtimeEvents.CongregationCreated,
            new { CongregationId = notification.Congregation.Id },
            cancellationToken);
}
