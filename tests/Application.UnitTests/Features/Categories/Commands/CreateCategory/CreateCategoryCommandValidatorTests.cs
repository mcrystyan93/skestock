using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Categories.Commands.CreateCategory;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandValidatorTests
{
    private static CategoryTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CategoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CategoryTestDbContext(options);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenNameIsValidAndUnique()
    {
        await using var context = CreateContext();
        var validator = new CreateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateCategoryCommand { Name = "Stationery" });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenNameIsEmpty()
    {
        await using var context = CreateContext();
        var validator = new CreateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateCategoryCommand { Name = "" });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenNameIsWhitespaceOnly()
    {
        await using var context = CreateContext();
        var validator = new CreateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateCategoryCommand { Name = "   " });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveMaxLengthErrorWhenNameExceedsMaximum()
    {
        await using var context = CreateContext();
        var validator = new CreateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateCategoryCommand { Name = new string('a', 101) });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [Test]
    public async Task ShouldNotHaveMaxLengthErrorWhenNameIsExactlyAtMaximum()
    {
        await using var context = CreateContext();
        var validator = new CreateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateCategoryCommand { Name = new string('a', 100) });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenNameAlreadyExists()
    {
        await using var context = CreateContext();
        context.Categories.Add(new Category { Name = "Stationery" });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateCategoryCommand { Name = "Stationery" });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenNameDiffersOnlyByCase()
    {
        await using var context = CreateContext();
        context.Categories.Add(new Category { Name = "Stationery" });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateCategoryCommand { Name = "STATIONERY" });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenNameDiffersOnlyByWhitespace()
    {
        await using var context = CreateContext();
        context.Categories.Add(new Category { Name = "Stationery" });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateCategoryCommand { Name = "  Stationery  " });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateNameErrorWhenNameIsUniqueAmongExistingCategories()
    {
        await using var context = CreateContext();
        context.Categories.Add(new Category { Name = "Stationery" });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateCategoryCommand { Name = "Electronics" });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldNotRunDuplicateNameCheckWhenNameIsEmpty()
    {
        // DependentRules means the MustAsync uniqueness check is short-circuited once
        // NotEmpty/MaximumLength already failed - only one error should surface.
        await using var context = CreateContext();
        var validator = new CreateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateCategoryCommand { Name = "" });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }
}
