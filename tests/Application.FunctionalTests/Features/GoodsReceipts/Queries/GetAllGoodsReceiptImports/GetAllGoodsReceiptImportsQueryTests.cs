using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceiptImports;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.GoodsReceipts.Queries.GetAllGoodsReceiptImports;

public class GetAllGoodsReceiptImportsQueryTests : TestBase
{
    /// <summary>
    /// Every test seeds its own SchoolClass tagged with a unique <see cref="_prefix"/> and always
    /// filters on it via <c>SearchTerm</c> (which matches Class.Name). This keeps assertions
    /// correct regardless of leftover rows from other tests/fixtures, and keeps each test's cache
    /// key distinct so cached results from previous tests against the same long-lived in-process
    /// HybridCache instance can't bleed into this one.
    /// </summary>
    private string _prefix = null!;
    private UserProfile _uploadedBy = null!;

    [SetUp]
    public async Task SetUpPrerequisites()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];

        _uploadedBy = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        await TestApp.AddAsync(_uploadedBy);
    }

    private async Task<SchoolClass> SeedSchoolClassAsync(string name)
    {
        var schoolClass = new SchoolClass
        {
            Name = $"{_prefix}-{name}",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 6, 1)
        };
        await TestApp.AddAsync(schoolClass);
        return schoolClass;
    }

    private async Task<FileMetadata> SeedFileMetadataAsync()
    {
        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = $"{_prefix}-receipt.pdf",
            BlobContainer = "app-files",
            BlobPath = $"imports/{_prefix}/receipt.pdf",
            ContentType = "application/pdf",
            SizeBytes = 2048,
            Status = FileStatus.Completed
        };
        await TestApp.AddAsync(file);
        return file;
    }

    private async Task<GoodsReceiptImport> SeedImportAsync(
        SchoolClass schoolClass, FileMetadata file, GoodsReceiptImportStatus status = GoodsReceiptImportStatus.Processing,
        DateTime? uploadedAt = null)
    {
        // Only FK scalars are set here (Class/FileMetadata/UploadedByUser navigations are left at
        // their null! default) - schoolClass/file/_uploadedBy were persisted via previous
        // TestApp.AddAsync calls using different DbContext scopes, so they're detached here.
        // Assigning them as navigations would make EF's Add() cascade Added state onto them too,
        // causing SaveChangesAsync to try to re-insert already-existing rows.
        var import = new GoodsReceiptImport
        {
            ClassId = schoolClass.Id,
            Class = null!,
            FileMetadataId = file.Id,
            FileMetadata = null!,
            UploadedByUserId = _uploadedBy.IdentityId,
            UploadedByUser = null!,
            BlobPath = file.BlobPath,
            Status = status,
            UploadedAt = uploadedAt ?? new DateTime(2024, 1, 1)
        };

        await TestApp.AddAsync(import);
        return import;
    }

    private static GetAllGoodsReceiptImportsQuery Query(
        string searchTerm,
        int pageSize = PaginationConstants.DEFAULT_PAGE_SIZE,
        string? cursor = null,
        List<PaginationSort>? sort = null,
        List<ColumnFilter>? filters = null) =>
        new()
        {
            SearchTerm = searchTerm,
            PageSize = pageSize,
            Cursor = cursor,
            Sort = sort ?? [],
            Filters = filters ?? []
        };

    [Test]
    public async Task Handle_ReturnsOnlySeededImportsMatchingSearchTerm()
    {
        var schoolClass = await SeedSchoolClassAsync("Class");
        var file = await SeedFileMetadataAsync();
        await SeedImportAsync(schoolClass, file);

        var result = await TestApp.SendAsync(Query(_prefix));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Select(i => i.ClassName).ShouldAllBe(name => name.StartsWith(_prefix));
    }

    [Test]
    public async Task Handle_WithNoMatches_ReturnsEmptyPage()
    {
        var result = await TestApp.SendAsync(Query($"{_prefix}-does-not-exist"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ReturnsDtoWithClassNameAndUploadedByName()
    {
        var schoolClass = await SeedSchoolClassAsync("Class");
        var file = await SeedFileMetadataAsync();
        var import = await SeedImportAsync(schoolClass, file);

        var result = await TestApp.SendAsync(Query(_prefix));

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value.Data.Single();
        dto.Id.ShouldBe(import.Id);
        dto.ClassId.ShouldBe(schoolClass.Id);
        dto.ClassName.ShouldBe(schoolClass.Name);
        dto.FileMetadataId.ShouldBe(file.Id);
        dto.BlobPath.ShouldBe(file.BlobPath);
        dto.Status.ShouldBe(GoodsReceiptImportStatus.Processing);
        dto.UploadedByName.ShouldBe(_uploadedBy.FullName);
    }

    [Test]
    public async Task Handle_WithClassIdFilter_ReturnsOnlyImportsForThatClass()
    {
        var classA = await SeedSchoolClassAsync("ClassA");
        var classB = await SeedSchoolClassAsync("ClassB");
        var file = await SeedFileMetadataAsync();
        await SeedImportAsync(classA, file);
        await SeedImportAsync(classB, file);

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("classId", FilterOperator.Equals, classA.Id)]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().ClassId.ShouldBe(classA.Id);
    }

    [Test]
    public async Task Handle_WithStatusFilter_ReturnsOnlyMatchingImports()
    {
        var schoolClass = await SeedSchoolClassAsync("Class");
        var file = await SeedFileMetadataAsync();
        await SeedImportAsync(schoolClass, file, GoodsReceiptImportStatus.Processing);
        await SeedImportAsync(schoolClass, file, GoodsReceiptImportStatus.PendingReview);

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("status", FilterOperator.Equals, (int)GoodsReceiptImportStatus.PendingReview)]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Status.ShouldBe(GoodsReceiptImportStatus.PendingReview);
    }

    [Test]
    public async Task Handle_WithUploadedAtSortAscending_ReturnsItemsInUploadedAtOrder()
    {
        var schoolClass = await SeedSchoolClassAsync("Class");
        var file = await SeedFileMetadataAsync();
        await SeedImportAsync(schoolClass, file, uploadedAt: new DateTime(2024, 3, 1));
        await SeedImportAsync(schoolClass, file, uploadedAt: new DateTime(2024, 1, 1));
        await SeedImportAsync(schoolClass, file, uploadedAt: new DateTime(2024, 2, 1));

        var result = await TestApp.SendAsync(Query(
            _prefix,
            sort: [new PaginationSort { Key = "uploadedAt", Value = "ascend" }]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(i => i.UploadedAt).ShouldBe(
            [new DateTime(2024, 1, 1), new DateTime(2024, 2, 1), new DateTime(2024, 3, 1)]);
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllSeededImportsExactlyOnce()
    {
        var schoolClass = await SeedSchoolClassAsync("Class");
        var file = await SeedFileMetadataAsync();

        for (var i = 0; i < 7; i++)
            await SeedImportAsync(schoolClass, file);

        var collected = new List<GoodsReceiptImportListItemDto>();
        string? cursor = null;
        var safety = 0;

        while (true)
        {
            safety++;
            safety.ShouldBeLessThan(20);

            var result = await TestApp.SendAsync(Query(
                _prefix,
                pageSize: 3,
                cursor: cursor,
                sort: [new PaginationSort { Key = "id", Value = "ascend" }]));

            result.IsSuccess.ShouldBeTrue();
            collected.AddRange(result.Value.Data);

            if (!result.Value.HasNextPage)
                break;

            cursor = result.Value.NextCursor;
        }

        collected.Select(i => i.Id).Distinct().Count().ShouldBe(7);
    }

    [Test]
    public async Task Handle_WithInvalidPageSize_ThrowsValidationException()
    {
        var schoolClass = await SeedSchoolClassAsync("Class");
        var file = await SeedFileMetadataAsync();
        await SeedImportAsync(schoolClass, file);

        var act = async () => await TestApp.SendAsync(Query(_prefix, pageSize: 0));

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(GetAllGoodsReceiptImportsQuery.PageSize));
    }

    [Test]
    public async Task Handle_WithMalformedCursor_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(Query(_prefix, cursor: "not-a-valid-cursor"));

        await act.ShouldThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Handle_WithCursorFromDifferentSort_ThrowsValidationException()
    {
        var schoolClass = await SeedSchoolClassAsync("Class");
        var file = await SeedFileMetadataAsync();
        await SeedImportAsync(schoolClass, file, uploadedAt: new DateTime(2024, 1, 1));
        await SeedImportAsync(schoolClass, file, uploadedAt: new DateTime(2024, 2, 1));
        await SeedImportAsync(schoolClass, file, uploadedAt: new DateTime(2024, 3, 1));

        var firstPage = await TestApp.SendAsync(Query(
            _prefix,
            pageSize: 1,
            sort: [new PaginationSort { Key = "uploadedAt", Value = "ascend" }]));

        firstPage.Value.HasNextPage.ShouldBeTrue();

        var act = async () => await TestApp.SendAsync(Query(
            _prefix,
            cursor: firstPage.Value.NextCursor,
            sort: [new PaginationSort { Key = "uploadedAt", Value = "descend" }]));

        await act.ShouldThrowAsync<ValidationException>();
    }
}
