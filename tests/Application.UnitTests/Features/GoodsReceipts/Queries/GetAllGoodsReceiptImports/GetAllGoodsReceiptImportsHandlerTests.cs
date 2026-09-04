using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceiptImports;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Queries.GetAllGoodsReceiptImports;

public class GetAllGoodsReceiptImportsHandlerTests
{
    private static async Task<(GoodsReceiptImportTestDbContext Context, SchoolClass Class, FileMetadata File, UserProfile UserProfile)> SeedPrerequisitesAsync(
        GoodsReceiptImportTestDbContext? context = null)
    {
        context ??= NewContext();

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        context.SchoolClasses.Add(schoolClass);

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "receipt.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/2026/receipt.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Status = FileStatus.Completed
        };
        context.FileMetadata.Add(file);

        var userProfile = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        context.UserProfiles.Add(userProfile);

        await context.SaveChangesAsync(CancellationToken.None);
        return (context, schoolClass, file, userProfile);
    }

    private static GoodsReceiptImportTestDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GoodsReceiptImportTestDbContext(options);
    }

    private static GoodsReceiptImport MakeImport(
        SchoolClass schoolClass, FileMetadata file, UserProfile uploadedBy,
        DateTimeOffset createdDate, GoodsReceiptImportStatus status = GoodsReceiptImportStatus.Processing,
        DateTime? uploadedAt = null) => new()
    {
        ClassId = schoolClass.Id,
        Class = schoolClass,
        FileMetadataId = file.Id,
        FileMetadata = file,
        UploadedByUserId = uploadedBy.IdentityId,
        UploadedByUser = uploadedBy,
        BlobPath = file.BlobPath,
        Status = status,
        UploadedAt = uploadedAt ?? createdDate.UtcDateTime,
        CreatedDate = createdDate,
        LastModifiedDate = createdDate
    };

    private static GetAllGoodsReceiptImportsHandler CreateHandler(IApplicationDbContext context) => new(context);

    // ---- Basic pagination -------------------------------------------------

    [Test]
    public async Task Handle_WithNoCursor_ReturnsFirstPageOrderedByDefaultSort()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 5; i++)
            context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, baseline.AddMinutes(i)));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllGoodsReceiptImportsQuery { PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var page = result.Value;
        page.Data.Count().ShouldBe(5);
        page.HasNextPage.ShouldBeFalse();
        page.NextCursor.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Handle_WithPageSizeSmallerThanTotal_SetsHasNextPageAndReturnsCursor()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 5; i++)
            context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, baseline.AddMinutes(i)));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllGoodsReceiptImportsQuery { PageSize = 2 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var page = result.Value;
        page.Data.Count().ShouldBe(2);
        page.HasNextPage.ShouldBeTrue();
        page.NextCursor.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllItemsExactlyOnceInOrder()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 11; i++)
            context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, baseline.AddMinutes(i)));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var collected = new List<GoodsReceiptImportListItemDto>();
        string? cursor = null;
        var safetyCounter = 0;

        while (true)
        {
            safetyCounter++.ShouldBeLessThan(20);

            var result = await handler.Handle(
                new GetAllGoodsReceiptImportsQuery { PageSize = 3, Cursor = cursor },
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            var page = result.Value;
            collected.AddRange(page.Data);

            if (!page.HasNextPage)
                break;

            page.NextCursor.ShouldNotBeNullOrWhiteSpace();
            cursor = page.NextCursor;
        }

        collected.Select(i => i.Id).Distinct().Count().ShouldBe(11);
    }

    [Test]
    public async Task Handle_WithEmptyDataSet_ReturnsEmptyPageWithNoCursor()
    {
        var context = NewContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllGoodsReceiptImportsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
        result.Value.NextCursor.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithPageSizeAboveMaximum_ClampsToDefaultPageSize()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 60; i++)
            context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, baseline.AddMinutes(i)));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllGoodsReceiptImportsQuery { PageSize = 1000 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(PaginationConstants.DEFAULT_PAGE_SIZE);
        result.Value.HasNextPage.ShouldBeTrue();
    }

    // ---- DTO shape ---------------------------------------------------------

    [Test]
    public async Task Handle_ReturnsDtoWithClassNameAndUploadedByName()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllGoodsReceiptImportsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value.Data.Single();
        dto.ClassId.ShouldBe(schoolClass.Id);
        dto.ClassName.ShouldBe(schoolClass.Name);
        dto.FileMetadataId.ShouldBe(file.Id);
        dto.BlobPath.ShouldBe(file.BlobPath);
        dto.Status.ShouldBe(GoodsReceiptImportStatus.Processing);
        dto.UploadedByName.ShouldBe(user.FullName);
    }

    // ---- Filtering -----------------------------------------------------------

    [Test]
    public async Task Handle_WithClassIdFilter_ReturnsOnlyImportsForThatClass()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        var otherClass = new SchoolClass { Name = "Spring 2027", StartDate = new DateOnly(2027, 1, 1), EndDate = new DateOnly(2027, 6, 1) };
        context.SchoolClasses.Add(otherClass);

        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow));
        context.GoodsReceiptImports.Add(MakeImport(otherClass, file, user, DateTimeOffset.UtcNow.AddMinutes(1)));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllGoodsReceiptImportsQuery
        {
            Filters = [new ColumnFilter("classId", FilterOperator.Equals, schoolClass.Id)]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().ClassId.ShouldBe(schoolClass.Id);
    }

    [Test]
    public async Task Handle_WithStatusFilter_ReturnsOnlyMatchingImports()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow, GoodsReceiptImportStatus.Processing));
        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow.AddMinutes(1), GoodsReceiptImportStatus.PendingReview));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllGoodsReceiptImportsQuery
        {
            Filters = [new ColumnFilter("status", FilterOperator.Equals, (int)GoodsReceiptImportStatus.PendingReview)]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Status.ShouldBe(GoodsReceiptImportStatus.PendingReview);
    }

    [Test]
    public async Task Handle_WithColumnFilterNotMatchingAnyRow_ReturnsEmptyPage()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllGoodsReceiptImportsQuery
        {
            Filters = [new ColumnFilter("classId", FilterOperator.Equals, Guid.NewGuid())]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WithSearchTerm_FiltersByClassName()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        var otherClass = new SchoolClass { Name = "UniqueClassNameXyz", StartDate = new DateOnly(2027, 1, 1), EndDate = new DateOnly(2027, 6, 1) };
        context.SchoolClasses.Add(otherClass);

        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow));
        context.GoodsReceiptImports.Add(MakeImport(otherClass, file, user, DateTimeOffset.UtcNow.AddMinutes(1)));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllGoodsReceiptImportsQuery { SearchTerm = "UniqueClassNameXyz" }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().ClassName.ShouldBe("UniqueClassNameXyz");
    }

    // ---- Sorting ------------------------------------------------------------

    [Test]
    public async Task Handle_WithUploadedAtSortAscending_ReturnsItemsInUploadedAtOrder()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow, uploadedAt: new DateTime(2024, 3, 1)));
        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow.AddMinutes(1), uploadedAt: new DateTime(2024, 1, 1)));
        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow.AddMinutes(2), uploadedAt: new DateTime(2024, 2, 1)));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllGoodsReceiptImportsQuery
        {
            Sort = [new PaginationSort { Key = "uploadedAt", Value = "ascend" }]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(i => i.UploadedAt).ShouldBe([new DateTime(2024, 1, 1), new DateTime(2024, 2, 1), new DateTime(2024, 3, 1)]);
        result.Value.Sort.ShouldContain(s => s.Key == "uploadedAt" && s.Value == "ascend");
    }

    [Test]
    public async Task Handle_WithUnknownSortKey_FallsBackToDefaultSort()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 3; i++)
            context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, baseline.AddMinutes(i)));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllGoodsReceiptImportsQuery
        {
            Sort = [new PaginationSort { Key = "totallyUnknownKey", Value = "ascend" }]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        // Falls back to default sort (CreatedDate desc, Id desc).
        result.Value.Data.Count().ShouldBe(3);
    }

    // ---- Cursor edge cases ----------------------------------------------------

    [Test]
    public async Task Handle_WithMalformedCursor_IsIgnoredAndReturnsFirstPage()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetAllGoodsReceiptImportsQuery { Cursor = "not-a-valid-cursor-token" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
    }

    [Test]
    public async Task Handle_WithEmptyCursor_ReturnsFirstPage()
    {
        var (context, schoolClass, file, user) = await SeedPrerequisitesAsync();
        context.GoodsReceiptImports.Add(MakeImport(schoolClass, file, user, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetAllGoodsReceiptImportsQuery { Cursor = "" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
    }
}
