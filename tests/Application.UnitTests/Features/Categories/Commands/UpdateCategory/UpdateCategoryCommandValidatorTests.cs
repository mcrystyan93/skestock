using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Categories.Commands.UpdateCategory;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;
using skestock.Domain.Entities.Categories;

namespace skestock.Application.UnitTests.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandValidatorTests
{
    private static CategoryTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CategoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CategoryTestDbContext(options);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenCommandIsValid()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = category.Id, Name = "New Stationery" });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmpty()
    {
        await using var context = CreateContext();
        var validator = new UpdateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = Guid.Empty, Name = "Stationery" });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateCategoryCommand.Id) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenNameIsEmpty()
    {
        await using var context = CreateContext();
        var validator = new UpdateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = Guid.NewGuid(), Name = "" });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateCategoryCommand.Name) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenNameIsWhitespaceOnly()
    {
        await using var context = CreateContext();
        var validator = new UpdateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = Guid.NewGuid(), Name = "   " });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveMaxLengthErrorWhenNameExceedsMaximum()
    {
        await using var context = CreateContext();
        var validator = new UpdateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = Guid.NewGuid(), Name = new string('a', 101) });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [Test]
    public async Task ShouldNotHaveMaxLengthErrorWhenNameIsExactlyAtMaximum()
    {
        await using var context = CreateContext();
        var validator = new UpdateCategoryCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = Guid.NewGuid(), Name = new string('a', 100) });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateNameErrorWhenNameBelongsToTheSameCategory()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = category.Id, Name = "Stationery" });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenNameBelongsToAnotherCategory()
    {
        await using var context = CreateContext();
        var existing = new Category { Name = "Stationery" };
        var toUpdate = new Category { Name = "Electronics" };
        context.Categories.AddRange(existing, toUpdate);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = toUpdate.Id, Name = "Stationery" });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenNameDiffersOnlyByCase()
    {
        await using var context = CreateContext();
        var existing = new Category { Name = "Stationery" };
        var toUpdate = new Category { Name = "Electronics" };
        context.Categories.AddRange(existing, toUpdate);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = toUpdate.Id, Name = "STATIONERY" });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveSelfReferenceErrorWhenParentIdEqualsOwnId()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Dairy" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = category.Id, Name = "Dairy", ParentId = category.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.SelfReference);
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenParentDoesNotExist()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Dairy" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = category.Id, Name = "Dairy", ParentId = Guid.NewGuid() });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldHaveCircularReferenceErrorWhenParentIsADescendant()
    {
        // Hierarchy: A -> B -> C. Setting A's parent to C (its own grandchild) is circular.
        await using var context = CreateContext();
        var a = new Category { Name = "A" };
        context.Categories.Add(a);
        await context.SaveChangesAsync(CancellationToken.None);
        var b = new Category { Name = "B", ParentId = a.Id };
        context.Categories.Add(b);
        await context.SaveChangesAsync(CancellationToken.None);
        var c = new Category { Name = "C", ParentId = b.Id };
        context.Categories.Add(c);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = a.Id, Name = "A", ParentId = c.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.CircularReference);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenParentIsUnrelated()
    {
        await using var context = CreateContext();
        var a = new Category { Name = "A" };
        var unrelated = new Category { Name = "Unrelated" };
        context.Categories.AddRange(a, unrelated);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateCategoryCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateCategoryCommand { Id = a.Id, Name = "A", ParentId = unrelated.Id });

        result.IsValid.ShouldBeTrue();
    }
}
