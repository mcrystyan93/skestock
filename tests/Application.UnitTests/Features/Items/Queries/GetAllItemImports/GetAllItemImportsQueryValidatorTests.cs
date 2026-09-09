using skestock.Application.Common.Errors;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items;
using skestock.Application.Features.Items.Queries.GetAllItemImports;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Queries.GetAllItemImports;

public class GetAllItemImportsQueryValidatorTests
{
    private readonly GetAllItemImportsQueryValidator _validator = new();
    private static readonly IKeysetSortConfiguration<ItemImport> SortConfiguration = new ItemImportSortConfiguration();

    private static string BuildCursor(List<PaginationSort> sort)
    {
        var effectiveSort = DynamicSortBuilder<ItemImport>.BuildEffectiveSort(sort, SortConfiguration);
        var entity = new ItemImport
        {
            Id = Guid.NewGuid(),
            FileMetadataId = Guid.NewGuid(),
            FileMetadata = null!,
            UploadedByUserId = Guid.NewGuid(),
            UploadedByUser = null!,
            BlobPath = "imports/items.pdf",
            CreatedDate = DateTimeOffset.UtcNow,
            LastModifiedDate = DateTimeOffset.UtcNow
        };

        return CursorCodec<ItemImport>.Encode(entity, effectiveSort, SortConfiguration)!;
    }

    [Test]
    public async Task ShouldNotHaveCursorSortMismatchErrorWhenCursorMatchesRequestedSort()
    {
        var sort = new List<PaginationSort> { new() { Key = "uploadedAt", Value = "asc" } };
        var query = new GetAllItemImportsQuery { Sort = sort, Cursor = BuildCursor(sort) };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldHaveCursorSortMismatchErrorWhenSortKeyChanges()
    {
        var cursor = BuildCursor([new() { Key = "uploadedAt", Value = "asc" }]);
        var query = new GetAllItemImportsQuery
        {
            Sort = [new() { Key = "createdDate", Value = "desc" }],
            Cursor = cursor
        };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.CursorSortMismatch);
    }

    [Test]
    public async Task ShouldHaveInvalidSortKeyErrorForUnknownSortKey()
    {
        var query = new GetAllItemImportsQuery { Sort = [new() { Key = "unknown", Value = "asc" }] };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidSortKey);
    }

    [Test]
    public async Task ShouldHavePageSizeErrorWhenOutOfRange()
    {
        var query = new GetAllItemImportsQuery { PageSize = 0 };

        var result = await _validator.ValidateAsync(query);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Between);
    }
}
