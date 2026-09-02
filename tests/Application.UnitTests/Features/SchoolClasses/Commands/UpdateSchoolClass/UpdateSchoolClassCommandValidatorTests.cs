using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.SchoolClasses.Commands.UpdateSchoolClass;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Commands.UpdateSchoolClass;

public class UpdateSchoolClassCommandValidatorTests
{
    private static async Task<(SchoolClassTestDbContext Context, SchoolClass SchoolClass)> CreateContextWithClassAsync()
    {
        var options = new DbContextOptionsBuilder<SchoolClassTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SchoolClassTestDbContext(options);
        var schoolClass = new SchoolClass { Name = "Fall 2026", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 1) };
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, schoolClass);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenCommandIsValid()
    {
        var (context, schoolClass) = await CreateContextWithClassAsync();
        await using var _ = context;
        var validator = new UpdateSchoolClassCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateSchoolClassCommand
        {
            Id = schoolClass.Id,
            Name = "New Name",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1)
        });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmpty()
    {
        var (context, _) = await CreateContextWithClassAsync();
        await using var _disposable = context;
        var validator = new UpdateSchoolClassCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateSchoolClassCommand
        {
            Id = Guid.Empty,
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1)
        });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateSchoolClassCommand.Id) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateNameErrorWhenNameBelongsToSameClass()
    {
        var (context, schoolClass) = await CreateContextWithClassAsync();
        await using var _ = context;
        var validator = new UpdateSchoolClassCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateSchoolClassCommand
        {
            Id = schoolClass.Id,
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1)
        });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenNameBelongsToAnotherClass()
    {
        var (context, _) = await CreateContextWithClassAsync();
        await using var _ = context;
        var other = new SchoolClass { Name = "Spring 2027", StartDate = new DateOnly(2027, 1, 1), EndDate = new DateOnly(2027, 6, 1) };
        context.SchoolClasses.Add(other);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateSchoolClassCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateSchoolClassCommand
        {
            Id = other.Id,
            Name = "Fall 2026",
            StartDate = new DateOnly(2027, 1, 1),
            EndDate = new DateOnly(2027, 6, 1)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveInvalidDateRangeErrorWhenStartDateIsNotBeforeEndDate()
    {
        var (context, schoolClass) = await CreateContextWithClassAsync();
        await using var _ = context;
        var validator = new UpdateSchoolClassCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateSchoolClassCommand
        {
            Id = schoolClass.Id,
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 12, 1),
            EndDate = new DateOnly(2026, 9, 1)
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidDateRange);
    }

    [Test]
    public async Task ShouldHaveErrorWhenStatusIsNotDefined()
    {
        var (context, schoolClass) = await CreateContextWithClassAsync();
        await using var _ = context;
        var validator = new UpdateSchoolClassCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateSchoolClassCommand
        {
            Id = schoolClass.Id,
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1),
            Status = (ClassStatus)999
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidEnum);
    }
}
