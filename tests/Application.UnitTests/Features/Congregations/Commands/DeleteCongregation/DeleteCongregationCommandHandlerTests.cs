using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Congregations;
using skestock.Application.Features.Congregations.Commands.DeleteCongregation;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Events.Congregations;

namespace skestock.Application.UnitTests.Features.Congregations.Commands.DeleteCongregation;

public class DeleteCongregationCommandHandlerTests
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
        var congregation = new Congregation { Name = "Central", Email = "c@example.com", DeletedDate = deletedDate };
        context.Congregations.Add(congregation);
        await context.SaveChangesAsync(CancellationToken.None);
        congregation.ClearDomainEvents();
        return congregation;
    }

    [Test]
    public async Task Handle_WithExistingCongregation_SoftDeletesIt()
    {
        await using var context = CreateContext();
        var congregation = await SeedAsync(context);
        var before = DateTimeOffset.UtcNow;

        var result = await new DeleteCongregationCommandHandler(context).Handle(
            new DeleteCongregationCommand { Id = congregation.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var persisted = await context.Congregations.IgnoreQueryFilters().SingleAsync(c => c.Id == congregation.Id);
        persisted.DeletedDate.ShouldNotBeNull();
        persisted.DeletedDate.Value.ShouldBeGreaterThanOrEqualTo(before);
        (await context.Congregations.AnyAsync(c => c.Id == congregation.Id)).ShouldBeFalse();
    }

    [Test]
    public async Task Handle_RaisesDeletedEvent()
    {
        await using var context = CreateContext();
        var congregation = await SeedAsync(context);

        await new DeleteCongregationCommandHandler(context).Handle(
            new DeleteCongregationCommand { Id = congregation.Id }, CancellationToken.None);

        congregation.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<CongregationDeletedEvent>();
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await new DeleteCongregationCommandHandler(context).Handle(
            new DeleteCongregationCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is CongregationErrors.CongregationNotFound);
    }

    [Test]
    public async Task Handle_WhenAlreadyDeleted_ReturnsNotFoundAndKeepsOriginalDeletedDate()
    {
        await using var context = CreateContext();
        var deletedDate = DateTimeOffset.UtcNow.AddDays(-1);
        var congregation = await SeedAsync(context, deletedDate);

        var result = await new DeleteCongregationCommandHandler(context).Handle(
            new DeleteCongregationCommand { Id = congregation.Id }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is CongregationErrors.CongregationNotFound);
        (await context.Congregations.IgnoreQueryFilters().SingleAsync(c => c.Id == congregation.Id))
            .DeletedDate.ShouldBe(deletedDate);
    }

    [Test]
    public void Command_InvalidatesCongregationListTag()
    {
        new DeleteCongregationCommand().Tags.ShouldBe([CacheConstants.CongregationListTag]);
    }
}
