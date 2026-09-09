using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Queries.GetAllItemImports;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Queries.GetAllItemImports;

public class GetAllItemImportsHandlerTests
{
    private static ItemImportTestDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ItemImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ItemImportTestDbContext(options);
    }

    private static async Task<(ItemImportTestDbContext Context, FileMetadata File, UserProfile UserProfile)> SeedPrerequisitesAsync(
        ItemImportTestDbContext? context = null)
    {
        context ??= NewContext();

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "items.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/2026/items.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Status = FileStatus.Completed
        };
        context.FileMetadata.Add(file);

        var userProfile = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        context.UserProfiles.Add(userProfile);

        await context.SaveChangesAsync(CancellationToken.None);
        return (context, file, userProfile);
    }

    private static ItemImport MakeImport(
        FileMetadata file, UserProfile uploadedBy, DateTimeOffset createdDate,
        ItemImportStatus status = ItemImportStatus.Processing, string? blobPath = null, string? errorMessage = null) => new()
    {
        FileMetadataId = file.Id,
        FileMetadata = file,
        UploadedByUserId = uploadedBy.IdentityId,
        UploadedByUser = uploadedBy,
        BlobPath = blobPath ?? file.BlobPath,
        Status = status,
        ErrorMessage = errorMessage,
        UploadedAt = createdDate.UtcDateTime,
        CreatedDate = createdDate,
        LastModifiedDate = createdDate
    };

    private static GetAllItemImportsHandler CreateHandler(IApplicationDbContext context) => new(context);

    [Test]
    public async Task Handle_WithNoCursor_ReturnsFirstPageOrderedByDefaultSort()
    {
        var (context, file, user) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 5; i++)
            context.ItemImports.Add(MakeImport(file, user, baseline.AddMinutes(i)));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllItemImportsQuery { PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var page = result.Value;
        page.Data.Count().ShouldBe(5);
        page.HasNextPage.ShouldBeFalse();

        // Default sort is CreatedDate desc, Id desc - most recently created first.
        page.Data.First().CreatedDate.ShouldBe(baseline.AddMinutes(4));
    }

    [Test]
    public async Task Handle_WithPageSizeSmallerThanTotal_SetsHasNextPageAndReturnsCursor()
    {
        var (context, file, user) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 3; i++)
            context.ItemImports.Add(MakeImport(file, user, baseline.AddMinutes(i)));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllItemImportsQuery { PageSize = 2 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(2);
        result.Value.HasNextPage.ShouldBeTrue();
        result.Value.NextCursor.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Handle_FiltersByStatus()
    {
        var (context, file, user) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        context.ItemImports.Add(MakeImport(file, user, baseline, ItemImportStatus.Processing));
        context.ItemImports.Add(MakeImport(file, user, baseline.AddMinutes(1), ItemImportStatus.PendingReview));
        context.ItemImports.Add(MakeImport(file, user, baseline.AddMinutes(2), ItemImportStatus.Confirmed));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllItemImportsQuery
        {
            PageSize = 10,
            Filters = [new ColumnFilter("status", FilterOperator.Equals, ItemImportStatus.Confirmed.ToString())]
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().Status.ShouldBe(ItemImportStatus.Confirmed);
    }

    [Test]
    public async Task Handle_SearchTermMatchesErrorMessage()
    {
        var (context, file, user) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        context.ItemImports.Add(MakeImport(file, user, baseline, ItemImportStatus.Failed, errorMessage: "corrupt PDF"));
        context.ItemImports.Add(MakeImport(file, user, baseline.AddMinutes(1), ItemImportStatus.PendingReview));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetAllItemImportsQuery { PageSize = 10, SearchTerm = "corrupt" }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().ErrorMessage.ShouldBe("corrupt PDF");
    }

    [Test]
    public async Task Handle_FlattensUploaderName()
    {
        var (context, file, user) = await SeedPrerequisitesAsync();
        context.ItemImports.Add(MakeImport(file, user, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = CreateHandler(context);
        var result = await handler.Handle(new GetAllItemImportsQuery { PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Single().UploadedByName.ShouldBe(user.FullName);
    }
}
