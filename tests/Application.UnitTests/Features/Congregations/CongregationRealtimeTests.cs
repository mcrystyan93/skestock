using Moq;
using NUnit.Framework;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Application.Features.Congregations.EventHandlers;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Events.Congregations;

namespace skestock.Application.UnitTests.Features.Congregations;

public class CongregationRealtimeTests
{
    [Test]
    public async Task CreatedEventHandler_NotifiesCongregationsListGroup()
    {
        var congregation = new Congregation { Id = Guid.CreateVersion7(), Name = "C", Email = "c@example.com" };
        var notifier = new Mock<IRealtimeNotifier>();

        await new CongregationCreatedEventHandler(notifier.Object)
            .Handle(new CongregationCreatedEvent(congregation), CancellationToken.None);

        notifier.Verify(n => n.NotifyGroupAsync(
            RealtimeGroups.CongregationsList,
            RealtimeEvents.CongregationCreated,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdatedEventHandler_NotifiesCongregationsListGroup()
    {
        var congregation = new Congregation { Id = Guid.CreateVersion7(), Name = "C", Email = "c@example.com" };
        var notifier = new Mock<IRealtimeNotifier>();

        await new CongregationUpdatedEventHandler(notifier.Object)
            .Handle(new CongregationUpdatedEvent(congregation), CancellationToken.None);

        notifier.Verify(n => n.NotifyGroupAsync(
            RealtimeGroups.CongregationsList,
            RealtimeEvents.CongregationUpdated,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeletedEventHandler_NotifiesCongregationsListGroup()
    {
        var congregation = new Congregation { Id = Guid.CreateVersion7(), Name = "C", Email = "c@example.com" };
        var notifier = new Mock<IRealtimeNotifier>();

        await new CongregationDeletedEventHandler(notifier.Object)
            .Handle(new CongregationDeletedEvent(congregation), CancellationToken.None);

        notifier.Verify(n => n.NotifyGroupAsync(
            RealtimeGroups.CongregationsList,
            RealtimeEvents.CongregationDeleted,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
