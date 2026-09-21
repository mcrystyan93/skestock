using NUnit.Framework;
using Shouldly;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Events.OrderList;

namespace skestock.Domain.UnitTests.Entities;

public class OrderListTests
{
    [Test]
    public void Create_ShouldStartAsDraftAndRaiseCreatedEvent()
    {
        var classId = Guid.NewGuid();

        var orderList = OrderList.Create(classId, "Weekly order", "note");

        orderList.ClassId.ShouldBe(classId);
        orderList.Name.ShouldBe("Weekly order");
        orderList.Note.ShouldBe("note");
        orderList.Status.ShouldBe(OrderListStatus.Draft);
        orderList.SubmittedAt.ShouldBeNull();
        orderList.IsEditable.ShouldBeTrue();
        orderList.DomainEvents.OfType<OrderListCreatedEvent>().ShouldHaveSingleItem();
    }

    [Test]
    public void Submit_ShouldTransitionToSubmittedSetTimestampAndRaiseEvent()
    {
        var orderList = OrderList.Create(Guid.NewGuid(), null, null);

        orderList.Submit();

        orderList.Status.ShouldBe(OrderListStatus.Submitted);
        orderList.SubmittedAt.ShouldNotBeNull();
        orderList.IsEditable.ShouldBeFalse();
        orderList.DomainEvents.OfType<OrderListSubmittedEvent>().ShouldHaveSingleItem();
    }

    [Test]
    public void Cancel_ShouldTransitionToCancelledAndRaiseEvent()
    {
        var orderList = OrderList.Create(Guid.NewGuid(), null, null);

        orderList.Cancel();

        orderList.Status.ShouldBe(OrderListStatus.Cancelled);
        orderList.IsEditable.ShouldBeFalse();
        orderList.DomainEvents.OfType<OrderListCancelledEvent>().ShouldHaveSingleItem();
    }

    [Test]
    public void Reopen_ShouldTransitionCancelledToDraftClearTimestampAndRaiseEvent()
    {
        var orderList = OrderList.Create(Guid.NewGuid(), null, null);
        orderList.Submit();
        orderList.Cancel();

        orderList.SubmittedAt.ShouldNotBeNull();

        orderList.Reopen();

        orderList.Status.ShouldBe(OrderListStatus.Draft);
        orderList.SubmittedAt.ShouldBeNull();
        orderList.IsEditable.ShouldBeTrue();
        orderList.IsReopenable.ShouldBeFalse();
        orderList.DomainEvents.OfType<OrderListReopenedEvent>().ShouldHaveSingleItem();
    }
}
