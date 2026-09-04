using skestock.Application.Common.Errors;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.GoodsReceipts;
using skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceiptImports;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Queries.GetAllGoodsReceiptImports;

public class GetAllGoodsReceiptImportsQueryValidatorTests
{
    private readonly GetAllGoodsReceiptImportsQueryValidator _validator = new();
    private static readonly IKeysetSortConfiguration<GoodsReceiptImport> SortConfiguration = new GoodsReceiptImportSortConfiguration();

    private static string BuildCursor(List<PaginationSort> sort)
    {
        var effectiveSort = DynamicSortBuilder<GoodsReceiptImport>.BuildEffectiveSort(sort, SortConfiguration);
        var entity = new GoodsReceiptImport
        {
            Id = Guid.NewGuid(),
            ClassId = Guid.NewGuid(),
            Class = null!,
            FileMetadataId = Guid.NewGuid(),
            FileMetadata = null!,
            UploadedByUserId = Guid.NewGuid(),
            UploadedByUser = null!,
            BlobPath = "imports/receipt.pdf",
            CreatedDate = DateTimeOffset.UtcNow,
            LastModifiedDate = DateTimeOffset.UtcNow
        };

        return CursorCodec<GoodsReceiptImport>.Encode(entity, effectiveSort, SortConfiguration)!;
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenCursorMatchesRequestedSort()
    {
        var sort = new List<PaginationSort> { new() { Key = "uploadedAt", Value = "asc" } };
        var query = new GetAllGoodsReceiptImportsQuery { Sort = sort, Cursor = BuildCursor(sort) };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldHaveCursorSortMismatchErrorWhenSortKeyChanges()
    {
        var cursor = BuildCursor([new() { Key = "uploadedAt", Value = "asc" }]);
        var query = new GetAllGoodsReceiptImportsQuery
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
        var cursor = BuildCursor([new() { Key = "uploadedAt", Value = "asc" }]);
        var query = new GetAllGoodsReceiptImportsQuery
        {
            Sort = [new() { Key = "uploadedAt", Value = "desc" }],
            Cursor = cursor
        };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenCursorIsAbsent()
    {
        var query = new GetAllGoodsReceiptImportsQuery { Sort = [], Cursor = null };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldOnlyReportInvalidCursorWhenCursorIsMalformed()
    {
        var query = new GetAllGoodsReceiptImportsQuery { Sort = [], Cursor = "not-a-valid-cursor" };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidCursor);
        // The sort-mismatch rule must not also fire for a cursor that already failed to decode.
        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenDefaultSortIsUsedOnBothSides()
    {
        var cursor = BuildCursor([]);
        var query = new GetAllGoodsReceiptImportsQuery { Sort = [], Cursor = cursor };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldHaveInvalidSortKeyErrorWhenSortKeyIsNotWhitelisted()
    {
        var query = new GetAllGoodsReceiptImportsQuery
        {
            Sort = [new PaginationSort { Key = "unknownKey", Value = "asc" }]
        };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidSortKey);
    }

    [Test]
    public async Task ShouldHaveBetweenErrorWhenPageSizeIsOutOfRange()
    {
        var query = new GetAllGoodsReceiptImportsQuery { PageSize = 0 };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Between);
    }
}
