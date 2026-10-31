using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Congregations.Commands.CreateCongregation;
using skestock.Application.Features.Congregations.Commands.UpdateCongregation;
using skestock.Domain.Entities.Congregations;

namespace skestock.Application.UnitTests.Features.Congregations;

public class CongregationCommandSupportTests
{
    private sealed class FailingSaveDbContext(DbContextOptions<CongregationTestDbContext> options)
        : CongregationTestDbContext(options)
    {
        public bool FailNextSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            FailNextSave
                ? throw new DbUpdateException("unique index violation")
                : base.SaveChangesAsync(cancellationToken);
    }

    private static FailingSaveDbContext CreateFailingContext() =>
        new(new DbContextOptionsBuilder<CongregationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CreateCongregationCommand CreateCommand(string name) => new()
    {
        Name = name,
        Email = "c@example.com"
    };

    [Test]
    public async Task Create_MapsUniqueIndexRaceToDuplicateNameValidationError()
    {
        await using var context = CreateFailingContext();
        context.Congregations.Add(new Congregation { Name = "Central", Email = "a@example.com" });
        await context.SaveChangesAsync(CancellationToken.None);
        context.FailNextSave = true;

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            new CreateCongregationCommandHandler(context).Handle(CreateCommand("central"), CancellationToken.None).AsTask());

        exception.Errors.ShouldContainKey(nameof(Congregation.Name));
        exception.Errors[nameof(Congregation.Name)].ShouldContain(e => e.Code == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task Create_RethrowsDbUpdateExceptionWhenNameIsNotTaken()
    {
        await using var context = CreateFailingContext();
        context.FailNextSave = true;

        await Should.ThrowAsync<DbUpdateException>(() =>
            new CreateCongregationCommandHandler(context).Handle(CreateCommand("Unique"), CancellationToken.None).AsTask());
    }

    [Test]
    public async Task Update_MapsUniqueIndexRaceToDuplicateNameValidationError()
    {
        await using var context = CreateFailingContext();
        var other = new Congregation { Name = "Taken", Email = "a@example.com" };
        var target = new Congregation { Name = "Mine", Email = "b@example.com" };
        context.Congregations.AddRange(other, target);
        await context.SaveChangesAsync(CancellationToken.None);
        context.FailNextSave = true;

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            new UpdateCongregationCommandHandler(context).Handle(
                new UpdateCongregationCommand { Id = target.Id, Name = "taken", Email = "b@example.com" },
                CancellationToken.None).AsTask());

        exception.Errors[nameof(Congregation.Name)].ShouldContain(e => e.Code == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task Update_RethrowsDbUpdateExceptionWhenOnlyItsOwnNameMatches()
    {
        await using var context = CreateFailingContext();
        var target = new Congregation { Name = "Mine", Email = "b@example.com" };
        context.Congregations.Add(target);
        await context.SaveChangesAsync(CancellationToken.None);
        context.FailNextSave = true;

        await Should.ThrowAsync<DbUpdateException>(() =>
            new UpdateCongregationCommandHandler(context).Handle(
                new UpdateCongregationCommand { Id = target.Id, Name = "Mine", Email = "b@example.com" },
                CancellationToken.None).AsTask());
    }
}
