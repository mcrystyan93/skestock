using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Commands.CreateSchoolClass;

public class CreateSchoolClassCommandValidatorTests
{
    private static SchoolClassTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SchoolClassTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SchoolClassTestDbContext(options);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenCommandIsValid()
    {
        await using var context = CreateContext();
        var validator = new CreateSchoolClassCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateSchoolClassCommand
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1),
            Status = ClassStatus.Upcoming
        });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenNameIsEmpty()
    {
        await using var context = CreateContext();
        var validator = new CreateSchoolClassCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateSchoolClassCommand
        {
            Name = "",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1)
        });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateSchoolClassCommand.Name) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenNameAlreadyExists()
    {
        await using var context = CreateContext();
        context.SchoolClasses.Add(new SchoolClass { Name = "Fall 2026", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 1) });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateSchoolClassCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateSchoolClassCommand
        {
            Name = "fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveInvalidDateRangeErrorWhenStartDateIsAfterEndDate()
    {
        await using var context = CreateContext();
        var validator = new CreateSchoolClassCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateSchoolClassCommand
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 12, 1),
            EndDate = new DateOnly(2026, 9, 1)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidDateRange);
    }

    [Test]
    public async Task ShouldHaveInvalidDateRangeErrorWhenStartDateEqualsEndDate()
    {
        await using var context = CreateContext();
        var validator = new CreateSchoolClassCommandValidator(context);

        var sameDate = new DateOnly(2026, 9, 1);
        var result = await validator.ValidateAsync(new CreateSchoolClassCommand
        {
            Name = "Fall 2026",
            StartDate = sameDate,
            EndDate = sameDate
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidDateRange);
    }

    [Test]
    public async Task ShouldHaveErrorWhenStatusIsNotDefined()
    {
        await using var context = CreateContext();
        var validator = new CreateSchoolClassCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateSchoolClassCommand
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1),
            Status = (ClassStatus)999
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidEnum);
    }

    [Test]
    public async Task ShouldHaveErrorWhenNameExceedsMaxLength()
    {
        await using var context = CreateContext();
        var validator = new CreateSchoolClassCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateSchoolClassCommand
        {
            Name = new string('a', 101),
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
    }
}
