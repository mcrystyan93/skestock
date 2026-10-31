using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Congregations.Commands.CreateCongregation;
using skestock.Domain.Entities.Congregations;

namespace skestock.Application.UnitTests.Features.Congregations.Commands.CreateCongregation;

public class CreateCongregationCommandValidatorTests
{
    private static CongregationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CongregationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CongregationTestDbContext(options);
    }

    private static CreateCongregationCommand ValidCommand() => new()
    {
        Name = "Central",
        Email = "central@example.com"
    };

    [Test]
    public async Task ShouldNotHaveErrorWhenOnlyRequiredFieldsAreSet()
    {
        await using var context = CreateContext();

        var result = await new CreateCongregationCommandValidator(context).ValidateAsync(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task ShouldHaveRequiredErrorWhenNameIsBlank(string name)
    {
        await using var context = CreateContext();

        var result = await new CreateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Name = name });

        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveMaxLengthErrorWhenNameExceedsMaximum()
    {
        await using var context = CreateContext();

        var result = await new CreateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Name = new string('a', 101) });

        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [Test]
    public async Task ShouldNotHaveMaxLengthErrorWhenNameIsExactlyAtMaximum()
    {
        await using var context = CreateContext();

        var result = await new CreateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Name = new string('a', 100) });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [TestCase("Stationery")]
    [TestCase("STATIONERY")]
    [TestCase("  Stationery  ")]
    public async Task ShouldHaveDuplicateNameErrorWhenNameMatchesExistingCongregation(string name)
    {
        await using var context = CreateContext();
        context.Congregations.Add(new Congregation { Name = "Stationery", Email = "a@example.com" });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new CreateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Name = name });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateNameErrorWhenExistingCongregationWithSameNameIsDeleted()
    {
        await using var context = CreateContext();
        context.Congregations.Add(new Congregation
        {
            Name = "Stationery",
            Email = "a@example.com",
            DeletedDate = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new CreateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Name = "Stationery" });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldNotRunDuplicateNameCheckWhenNameIsEmpty()
    {
        await using var context = CreateContext();

        var result = await new CreateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Name = "" });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task ShouldHaveRequiredErrorWhenEmailIsBlank(string email)
    {
        await using var context = CreateContext();

        var result = await new CreateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Email = email });

        result.Errors.ShouldContain(e => e.PropertyName == "Email" && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveEmailErrorWhenEmailIsNotValid()
    {
        await using var context = CreateContext();

        var result = await new CreateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Email = "not-an-email" });

        result.Errors.ShouldContain(e => e.PropertyName == "Email" && e.ErrorCode == ValidationErrorCodes.Email);
    }

    [Test]
    public async Task ShouldHaveMaxLengthErrorWhenEmailExceedsMaximum()
    {
        await using var context = CreateContext();

        var result = await new CreateCongregationCommandValidator(context)
            .ValidateAsync(ValidCommand() with { Email = new string('a', 195) + "@x.com" });

        result.Errors.ShouldContain(e => e.PropertyName == "Email" && e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [TestCase(nameof(CreateCongregationCommand.Coordinator), 101)]
    [TestCase(nameof(CreateCongregationCommand.DessertContactPerson), 101)]
    [TestCase(nameof(CreateCongregationCommand.CoordinatorPhone), 31)]
    [TestCase(nameof(CreateCongregationCommand.DessertContactPhone), 31)]
    [TestCase(nameof(CreateCongregationCommand.Message), 1001)]
    public async Task ShouldHaveMaxLengthErrorWhenOptionalFieldExceedsMaximum(string property, int length)
    {
        await using var context = CreateContext();
        var command = SetProperty(ValidCommand(), property, new string('a', length));

        var result = await new CreateCongregationCommandValidator(context).ValidateAsync(command);

        result.Errors.ShouldContain(e => e.PropertyName == property && e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [TestCase(nameof(CreateCongregationCommand.Coordinator), 100)]
    [TestCase(nameof(CreateCongregationCommand.DessertContactPerson), 100)]
    [TestCase(nameof(CreateCongregationCommand.CoordinatorPhone), 30)]
    [TestCase(nameof(CreateCongregationCommand.DessertContactPhone), 30)]
    [TestCase(nameof(CreateCongregationCommand.Message), 1000)]
    public async Task ShouldNotHaveErrorWhenOptionalFieldIsExactlyAtMaximum(string property, int length)
    {
        await using var context = CreateContext();
        var command = SetProperty(ValidCommand(), property, new string('a', length));

        var result = await new CreateCongregationCommandValidator(context).ValidateAsync(command);

        result.IsValid.ShouldBeTrue();
    }

    private static CreateCongregationCommand SetProperty(CreateCongregationCommand command, string property, string value) =>
        property switch
        {
            nameof(CreateCongregationCommand.Coordinator) => command with { Coordinator = value },
            nameof(CreateCongregationCommand.DessertContactPerson) => command with { DessertContactPerson = value },
            nameof(CreateCongregationCommand.CoordinatorPhone) => command with { CoordinatorPhone = value },
            nameof(CreateCongregationCommand.DessertContactPhone) => command with { DessertContactPhone = value },
            nameof(CreateCongregationCommand.Message) => command with { Message = value },
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
        };
}
