using skestock.Application.Common.Errors;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Categories;
using skestock.Application.Features.Categories.Queries.GetAllCategories;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Queries.GetAllCategories;

public class GetAllCategoriesQueryValidatorTests
{
    private readonly GetAllCategoriesQueryValidator _validator = new();
    private static readonly IKeysetSortConfiguration<Category> SortConfiguration = new CategorySortConfiguration();

    private static string BuildCursor(List<PaginationSort> sort)
    {
        var effectiveSort = DynamicSortBuilder<Category>.BuildEffectiveSort(sort, SortConfiguration);
        var entity = new Category
        {
            Id = 1,
            Name = "Test Category",
            CreatedDate = DateTimeOffset.UtcNow,
            LastModifiedDate = DateTimeOffset.UtcNow
        };

        return CursorCodec<Category>.Encode(entity, effectiveSort, SortConfiguration)!;
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenCursorMatchesRequestedSort()
    {
        var sort = new List<PaginationSort> { new() { Key = "name", Value = "asc" } };
        var query = new GetAllCategoriesQuery { Sort = sort, Cursor = BuildCursor(sort) };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldHaveCursorSortMismatchErrorWhenSortKeyChanges()
    {
        var cursor = BuildCursor([new() { Key = "name", Value = "asc" }]);
        var query = new GetAllCategoriesQuery
        {
            Sort = [new() { Key = "createdDate", Value = "desc" }],
            Cursor = cursor
        };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldHaveCursorSortMismatchErrorWhenDirectionChanges()
    {
        var cursor = BuildCursor([new() { Key = "name", Value = "asc" }]);
        var query = new GetAllCategoriesQuery
        {
            Sort = [new() { Key = "name", Value = "desc" }],
            Cursor = cursor
        };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenCursorIsAbsent()
    {
        var query = new GetAllCategoriesQuery { Sort = [], Cursor = null };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldOnlyReportInvalidCursorWhenCursorIsMalformed()
    {
        var query = new GetAllCategoriesQuery { Sort = [], Cursor = "not-a-valid-cursor" };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidCursor);
        // The sort-mismatch rule must not also fire for a cursor that already failed to decode.
        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenDefaultSortIsUsedOnBothSides()
    {
        var cursor = BuildCursor([]);
        var query = new GetAllCategoriesQuery { Sort = [], Cursor = cursor };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }
}
