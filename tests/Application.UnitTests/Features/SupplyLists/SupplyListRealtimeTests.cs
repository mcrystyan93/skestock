using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Realtime;
using skestock.Application.Features.SupplyLists.Commands.CreateSupplyList;
using skestock.Application.Features.SupplyLists.Commands.DisableSupplyList;
using skestock.Application.Features.SupplyLists.Commands.EnableSupplyList;
using skestock.Application.Features.SupplyLists.Commands.UpdateSupplyList;
using skestock.Application.Features.SupplyLists.EventHandlers;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Events.SupplyLists;

namespace skestock.Application.UnitTests.Features.SupplyLists;

public class SupplyListRealtimeTests
{
    [Test]
    public async Task Create_And_Update_RaiseDomainEvents()
    {
        var (context, rice, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;

        var created = await new CreateSupplyListCommandHandler(context).Handle(new CreateSupplyListCommand
        {
            Name = "Weekly", Frequency = SupplyListFrequency.Weekly
        }, CancellationToken.None);

        var entity = context.SupplyLists.Single(l => l.Id == created.Value.Id);
        entity.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SupplyListCreatedEvent>();
        entity.ClearDomainEvents();

        await new UpdateSupplyListCommandHandler(context).Handle(new UpdateSupplyListCommand
        {
            Id = entity.Id, Name = "Weekly 2", Frequency = SupplyListFrequency.Weekly
        }, CancellationToken.None);

        entity.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SupplyListUpdatedEvent>();
    }

    [Test]
    public async Task DisableEnable_RaiseEventsOnlyOnStateChange()
    {
        var (context, _, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;
        var list = new SupplyList { Name = "Existing", Frequency = SupplyListFrequency.Weekly };
        context.SupplyLists.Add(list);
        await context.SaveChangesAsync(CancellationToken.None);

        await new EnableSupplyListCommandHandler(context).Handle(new EnableSupplyListCommand { Id = list.Id }, CancellationToken.None);
        list.DomainEvents.ShouldBeEmpty();

        await new DisableSupplyListCommandHandler(context).Handle(new DisableSupplyListCommand { Id = list.Id }, CancellationToken.None);
        await new DisableSupplyListCommandHandler(context).Handle(new DisableSupplyListCommand { Id = list.Id }, CancellationToken.None);
        list.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SupplyListDisabledEvent>();
        list.ClearDomainEvents();

        await new EnableSupplyListCommandHandler(context).Handle(new EnableSupplyListCommand { Id = list.Id }, CancellationToken.None);
        list.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SupplyListEnabledEvent>();
    }

    [Test]
    public async Task EventHandlers_NotifySupplyListsGroup()
    {
        var list = new SupplyList { Id = Guid.CreateVersion7(), Name = "L" };
        var notifier = new Mock<IRealtimeNotifier>();

        await new SupplyListCreatedEventHandler(notifier.Object).Handle(new SupplyListCreatedEvent(list), CancellationToken.None);
        await new SupplyListUpdatedEventHandler(notifier.Object).Handle(new SupplyListUpdatedEvent(list), CancellationToken.None);
        await new SupplyListDisabledEventHandler(notifier.Object).Handle(new SupplyListDisabledEvent(list), CancellationToken.None);
        await new SupplyListEnabledEventHandler(notifier.Object).Handle(new SupplyListEnabledEvent(list), CancellationToken.None);

        foreach (var eventName in new[]
                 {
                     RealtimeEvents.SupplyListCreated, RealtimeEvents.SupplyListUpdated,
                     RealtimeEvents.SupplyListDisabled, RealtimeEvents.SupplyListEnabled
                 })
        {
            notifier.Verify(n => n.NotifyGroupAsync(
                RealtimeGroups.SupplyListsList, eventName, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
