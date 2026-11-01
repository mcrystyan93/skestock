using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Congregations;
using skestock.Application.Features.Congregations.Commands.CreateCongregation;
using skestock.Domain.Events.Congregations;

namespace skestock.Application.UnitTests.Features.Congregations.Commands.CreateCongregation;

public class CreateCongregationCommandHandlerTests
{
    private static CongregationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CongregationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CongregationTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithAllFields_PersistsCongregationAndReturnsDto()
    {
        await using var context = CreateContext();
        var handler = new CreateCongregationCommandHandler(context);

        var result = await handler.Handle(new CreateCongregationCommand
        {
            Name = "Central",
            Email = "central@example.com",
            Coordinator = "Ana Pop",
            CoordinatorPhone = "0711111111",
            DessertContactPerson = "Ion Ionescu",
            DessertContactPhone = "0722222222",
            CanReceiveStudents = true,
            IsSmallCongregation = true,
            Message = "Hello"
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var persisted = await context.Congregations.SingleAsync(c => c.Id == result.Value.Id);
        persisted.Name.ShouldBe("Central");
        persisted.Email.ShouldBe("central@example.com");
        persisted.Coordinator.ShouldBe("Ana Pop");
        persisted.CoordinatorPhone.ShouldBe("0711111111");
        persisted.DessertContactPerson.ShouldBe("Ion Ionescu");
        persisted.DessertContactPhone.ShouldBe("0722222222");
        persisted.CanReceiveStudents.ShouldBeTrue();
        persisted.IsSmallCongregation.ShouldBeTrue();
        persisted.Message.ShouldBe("Hello");
        result.Value.Name.ShouldBe("Central");
        result.Value.CanReceiveStudents.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithOnlyRequiredFields_DefaultsBooleansToFalse()
    {
        await using var context = CreateContext();
        var handler = new CreateCongregationCommandHandler(context);

        var result = await handler.Handle(
            new CreateCongregationCommand { Name = "Central", Email = "central@example.com" },
            CancellationToken.None);

        result.Value.CanReceiveStudents.ShouldBeFalse();
        result.Value.IsSmallCongregation.ShouldBeFalse();
        (await context.Congregations.SingleAsync()).DeletedDate.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithWhitespaceAroundText_TrimsBeforePersisting()
    {
        await using var context = CreateContext();
        var handler = new CreateCongregationCommandHandler(context);

        var result = await handler.Handle(new CreateCongregationCommand
        {
            Name = "  Central  ",
            Email = "  central@example.com ",
            Coordinator = " Ana "
        }, CancellationToken.None);

        result.Value.Name.ShouldBe("Central");
        result.Value.Email.ShouldBe("central@example.com");
        result.Value.Coordinator.ShouldBe("Ana");
    }

    [Test]
    public async Task Handle_RaisesCreatedEvent()
    {
        await using var context = CreateContext();
        var handler = new CreateCongregationCommandHandler(context);

        var result = await handler.Handle(
            new CreateCongregationCommand { Name = "Central", Email = "central@example.com" },
            CancellationToken.None);

        var entity = await context.Congregations.SingleAsync(c => c.Id == result.Value.Id);
        entity.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<CongregationCreatedEvent>();
    }

    [Test]
    public void Command_InvalidatesCongregationListTag()
    {
        new CreateCongregationCommand().Tags.ShouldBe([CacheConstants.CongregationListTag]);
    }
}
