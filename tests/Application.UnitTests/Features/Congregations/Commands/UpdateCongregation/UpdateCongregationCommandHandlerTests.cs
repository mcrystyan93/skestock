using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Congregations;
using skestock.Application.Features.Congregations.Commands.UpdateCongregation;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Events.Congregations;

namespace skestock.Application.UnitTests.Features.Congregations.Commands.UpdateCongregation;

public class UpdateCongregationCommandHandlerTests
{
    private static CongregationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CongregationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CongregationTestDbContext(options);
    }

    private static async Task<Congregation> SeedAsync(CongregationTestDbContext context, DateTimeOffset? deletedDate = null)
    {
        var congregation = new Congregation
        {
            Name = "Old",
            Email = "old@example.com",
            Coordinator = "Old Coordinator",
            CanReceiveStudents = true,
            IsSmallCongregation = true,
            Message = "Old message",
            DeletedDate = deletedDate
        };
        context.Congregations.Add(congregation);
        await context.SaveChangesAsync(CancellationToken.None);
        congregation.ClearDomainEvents();
        return congregation;
    }

    [Test]
    public async Task Handle_WithExistingCongregation_UpdatesAllFieldsAndReturnsDto()
    {
        await using var context = CreateContext();
        var congregation = await SeedAsync(context);

        var result = await new UpdateCongregationCommandHandler(context).Handle(new UpdateCongregationCommand
        {
            Id = congregation.Id,
            Name = "New",
            Email = "new@example.com",
            Coordinator = "Ana",
            CoordinatorPhone = "0711111111",
            DessertContactPerson = "Ion",
            DessertContactPhone = "0722222222",
            CanReceiveStudents = false,
            IsSmallCongregation = false,
            Message = null
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(congregation.Id);
        result.Value.Name.ShouldBe("New");

        var persisted = await context.Congregations.SingleAsync(c => c.Id == congregation.Id);
        persisted.Name.ShouldBe("New");
        persisted.Email.ShouldBe("new@example.com");
        persisted.Coordinator.ShouldBe("Ana");
        persisted.CoordinatorPhone.ShouldBe("0711111111");
        persisted.DessertContactPerson.ShouldBe("Ion");
        persisted.DessertContactPhone.ShouldBe("0722222222");
        persisted.CanReceiveStudents.ShouldBeFalse();
        persisted.IsSmallCongregation.ShouldBeFalse();
        persisted.Message.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithWhitespaceAroundText_TrimsBeforePersisting()
    {
        await using var context = CreateContext();
        var congregation = await SeedAsync(context);

        var result = await new UpdateCongregationCommandHandler(context).Handle(new UpdateCongregationCommand
        {
            Id = congregation.Id,
            Name = "  Trimmed  ",
            Email = " trimmed@example.com ",
            Coordinator = " Ana "
        }, CancellationToken.None);

        result.Value.Name.ShouldBe("Trimmed");
        result.Value.Email.ShouldBe("trimmed@example.com");
        result.Value.Coordinator.ShouldBe("Ana");
    }

    [Test]
    public async Task Handle_RaisesUpdatedEvent()
    {
        await using var context = CreateContext();
        var congregation = await SeedAsync(context);

        await new UpdateCongregationCommandHandler(context).Handle(
            new UpdateCongregationCommand { Id = congregation.Id, Name = "New", Email = "new@example.com" },
            CancellationToken.None);

        congregation.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<CongregationUpdatedEvent>();
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await new UpdateCongregationCommandHandler(context).Handle(
            new UpdateCongregationCommand { Id = Guid.NewGuid(), Name = "Any", Email = "a@example.com" },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is CongregationErrors.CongregationNotFound);
    }

    [Test]
    public async Task Handle_WithDeletedCongregation_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var congregation = await SeedAsync(context, DateTimeOffset.UtcNow);

        var result = await new UpdateCongregationCommandHandler(context).Handle(
            new UpdateCongregationCommand { Id = congregation.Id, Name = "New", Email = "new@example.com" },
            CancellationToken.None);

        result.Errors.ShouldContain(e => e is CongregationErrors.CongregationNotFound);
    }

    [Test]
    public void Command_InvalidatesCongregationListTag()
    {
        new UpdateCongregationCommand().Tags.ShouldBe([CacheConstants.CongregationListTag]);
    }
}
