using skestock.Application.Common.Errors;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items;
using skestock.Application.Features.Items.Queries.GetAllItems;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Queries.GetAllItems;

public class GetAllItemsQueryValidatorTests
{
    private readonly GetAllItemsQueryValidator _validator = new();
    private static readonly IKeysetSortConfiguration<Item> SortConfiguration = new ItemSortConfiguration();

    private static string BuildCursor(List<PaginationSort> sort)
    {
        var effectiveSort = DynamicSortBuilder<Item>.BuildEffectiveSort(sort, SortConfiguration);
        var entity = new Item
        {
            Id = 1,
            Name = "Test Item",
            Unit = "unit",
            CreatedDate = DateTimeOffset.UtcNow,
            LastModifiedDate = DateTimeOffset.UtcNow
        };

        return CursorCodec<Item>.Encode(entity, effectiveSort, SortConfiguration)!;
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenCursorMatchesRequestedSort()
    {
        var sort = new List<PaginationSort> { new() { Key = "name", Value = "asc" } };
        var query = new GetAllItemsQuery { Sort = sort, Cursor = BuildCursor(sort) };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldHaveCursorSortMismatchErrorWhenSortKeyChanges()
    {
        var cursor = BuildCursor([new() { Key = "name", Value = "asc" }]);
        var query = new GetAllItemsQuery
        {
            Sort = [new() { Key = "createdDate", Value = "desc" }],
            Cursor = cursor
        };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldHaveErrorWhenSortKeyIsUnknown()
    {
        var query = new GetAllItemsQuery { Sort = [new() { Key = "unknownKey", Value = "asc" }] };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidSortKey);
    }

    [Test]
    public async Task ShouldHaveErrorWhenPageSizeIsOutOfRange()
    {
        var query = new GetAllItemsQuery { PageSize = 0 };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Between);
    }

    [Test]
    public async Task ShouldOnlyReportInvalidCursorWhenCursorIsMalformed()
    {
        var query = new GetAllItemsQuery { Sort = [], Cursor = "not-a-valid-cursor" };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidCursor);
        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }
}
