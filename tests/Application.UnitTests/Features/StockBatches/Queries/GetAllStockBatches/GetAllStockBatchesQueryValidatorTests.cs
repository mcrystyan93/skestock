using skestock.Application.Common.Errors;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.StockBatches;
using skestock.Application.Features.StockBatches.Queries.GetAllStockBatches;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.StockBatches.Queries.GetAllStockBatches;

public class GetAllStockBatchesQueryValidatorTests
{
    private readonly GetAllStockBatchesQueryValidator _validator = new();
    private static readonly IKeysetSortConfiguration<StockBatch> SortConfiguration = new StockBatchSortConfiguration();

    private static string BuildCursor(List<PaginationSort> sort)
    {
        var effectiveSort = DynamicSortBuilder<StockBatch>.BuildEffectiveSort(sort, SortConfiguration);
        var entity = new StockBatch
        {
            Id = 1,
            Item = null!,
            Location = null!,
            ReceivedClass = null!,
            Quantity = 1,
            UnitPrice = 1m,
            ReceivedDate = new DateOnly(2024, 1, 1),
            CreatedDate = DateTimeOffset.UtcNow,
            LastModifiedDate = DateTimeOffset.UtcNow
        };

        return CursorCodec<StockBatch>.Encode(entity, effectiveSort, SortConfiguration)!;
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenCursorMatchesRequestedSort()
    {
        var sort = new List<PaginationSort> { new() { Key = "receivedDate", Value = "asc" } };
        var query = new GetAllStockBatchesQuery { Sort = sort, Cursor = BuildCursor(sort) };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldHaveCursorSortMismatchErrorWhenSortKeyChanges()
    {
        var cursor = BuildCursor([new() { Key = "receivedDate", Value = "asc" }]);
        var query = new GetAllStockBatchesQuery
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
        var cursor = BuildCursor([new() { Key = "receivedDate", Value = "asc" }]);
        var query = new GetAllStockBatchesQuery
        {
            Sort = [new() { Key = "receivedDate", Value = "desc" }],
            Cursor = cursor
        };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenCursorIsAbsent()
    {
        var query = new GetAllStockBatchesQuery { Sort = [], Cursor = null };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldOnlyReportInvalidCursorWhenCursorIsMalformed()
    {
        var query = new GetAllStockBatchesQuery { Sort = [], Cursor = "not-a-valid-cursor" };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidCursor);
        // The sort-mismatch rule must not also fire for a cursor that already failed to decode.
        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenDefaultSortIsUsedOnBothSides()
    {
        var cursor = BuildCursor([]);
        var query = new GetAllStockBatchesQuery { Sort = [], Cursor = cursor };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldHaveInvalidSortKeyErrorWhenSortKeyIsNotWhitelisted()
    {
        var query = new GetAllStockBatchesQuery
        {
            Sort = [new PaginationSort { Key = "unknownKey", Value = "asc" }]
        };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidSortKey);
    }

    [Test]
    public async Task ShouldHaveBetweenErrorWhenPageSizeIsOutOfRange()
    {
        var query = new GetAllStockBatchesQuery { PageSize = 0 };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Between);
    }
}
