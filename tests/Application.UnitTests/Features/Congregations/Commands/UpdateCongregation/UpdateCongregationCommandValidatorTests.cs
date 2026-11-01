using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Congregations.Commands.UpdateCongregation;
using skestock.Domain.Entities.Congregations;

namespace skestock.Application.UnitTests.Features.Congregations.Commands.UpdateCongregation;

public class UpdateCongregationCommandValidatorTests
{
    private static CongregationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CongregationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CongregationTestDbContext(options);
    }

    private static UpdateCongregationCommand ValidCommand() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Central",
        Email = "central@example.com"
    };

    [Test]
    public async Task ShouldNotHaveErrorWhenOnlyRequiredFieldsAreSet()
    {
        await using var context = CreateContext();

        var result = await new UpdateCongregationCommandValidator(context).ValidateAsync(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenIdIsEmpty()
    {
        await using var context = CreateContext();

        var result = await new UpdateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Id = Guid.Empty });

        result.Errors.ShouldContain(e => e.PropertyName == "Id" && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenNameIsBlank()
    {
        await using var context = CreateContext();

        var result = await new UpdateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Name = "  " });

        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveMaxLengthErrorWhenNameExceedsMaximum()
    {
        await using var context = CreateContext();

        var result = await new UpdateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Name = new string('a', 101) });

        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenAnotherCongregationHasTheName()
    {
        await using var context = CreateContext();
        context.Congregations.Add(new Congregation { Name = "Stationery", Email = "a@example.com" });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new UpdateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Name = "STATIONERY" });

        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateNameErrorWhenNameBelongsToTheSameCongregation()
    {
        await using var context = CreateContext();
        var existing = new Congregation { Name = "Stationery", Email = "a@example.com" };
        context.Congregations.Add(existing);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new UpdateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Id = existing.Id, Name = "Stationery" });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateNameErrorWhenOtherCongregationWithSameNameIsDeleted()
    {
        await using var context = CreateContext();
        context.Congregations.Add(new Congregation
        {
            Name = "Stationery",
            Email = "a@example.com",
            DeletedDate = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new UpdateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Name = "Stationery" });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenEmailIsBlank()
    {
        await using var context = CreateContext();

        var result = await new UpdateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Email = "" });

        result.Errors.ShouldContain(e => e.PropertyName == "Email" && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveEmailErrorWhenEmailIsNotValid()
    {
        await using var context = CreateContext();

        var result = await new UpdateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Email = "not-an-email" });

        result.Errors.ShouldContain(e => e.PropertyName == "Email" && e.ErrorCode == ValidationErrorCodes.Email);
    }

    [TestCase(nameof(UpdateCongregationCommand.Coordinator), 101)]
    [TestCase(nameof(UpdateCongregationCommand.DessertContactPerson), 101)]
    [TestCase(nameof(UpdateCongregationCommand.CoordinatorPhone), 31)]
    [TestCase(nameof(UpdateCongregationCommand.DessertContactPhone), 31)]
    [TestCase(nameof(UpdateCongregationCommand.Message), 1001)]
    public async Task ShouldHaveMaxLengthErrorWhenOptionalFieldExceedsMaximum(string property, int length)
    {
        await using var context = CreateContext();
        var value = new string('a', length);
        var command = property switch
        {
            nameof(UpdateCongregationCommand.Coordinator) => ValidCommand() with { Coordinator = value },
            nameof(UpdateCongregationCommand.DessertContactPerson) => ValidCommand() with { DessertContactPerson = value },
            nameof(UpdateCongregationCommand.CoordinatorPhone) => ValidCommand() with { CoordinatorPhone = value },
            nameof(UpdateCongregationCommand.DessertContactPhone) => ValidCommand() with { DessertContactPhone = value },
            _ => ValidCommand() with { Message = value }
        };

        var result = await new UpdateCongregationCommandValidator(context).ValidateAsync(command);

        result.Errors.ShouldContain(e => e.PropertyName == property && e.ErrorCode == ValidationErrorCodes.MaxLength);
    }
}
