using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;

public class FakeUser(Guid? id) : IUser
{
    public Guid? Id { get; } = id;
    public List<string>? Roles { get; } = [];
}

public class CreateGoodsReceiptImportCommandHandlerTests
{
    private static async Task<(GoodsReceiptImportTestDbContext Context, SchoolClass Class, FileMetadata File, UserProfile UserProfile)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new GoodsReceiptImportTestDbContext(options);

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

        // UploadedByUserId links to the caller's Identity/AspNetUsers id (UserProfile.IdentityId is
        // the FK's principal key - see GoodsReceiptImportConfiguration), so the profile only needs
        // to exist.
        var userProfile = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        context.UserProfiles.Add(userProfile);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, schoolClass, file, userProfile);
    }

    [Test]
    public async Task Handle_WithValidData_CreatesProcessingImportAndReturnsDto()
    {
        var (context, schoolClass, file, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateGoodsReceiptImportCommand
        {
            ClassId = schoolClass.Id,
            FileMetadataId = file.Id
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Status.ShouldBe(GoodsReceiptImportStatus.Processing);
        result.Value.ClassId.ShouldBe(schoolClass.Id);
        result.Value.FileMetadataId.ShouldBe(file.Id);
        result.Value.BlobPath.ShouldBe(file.BlobPath);
    }

    [Test]
    public async Task Handle_CopiesBlobPathFromFileMetadataAndStampsUploader()
    {
        var (context, schoolClass, file, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateGoodsReceiptImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new CreateGoodsReceiptImportCommand
        {
            ClassId = schoolClass.Id,
            FileMetadataId = file.Id
        }, CancellationToken.None);

        var persisted = await context.GoodsReceiptImports.SingleAsync(CancellationToken.None);
        persisted.Id.ShouldBe(result.Value.Id);
        persisted.Status.ShouldBe(GoodsReceiptImportStatus.Processing);
        persisted.ClassId.ShouldBe(schoolClass.Id);
        persisted.FileMetadataId.ShouldBe(file.Id);
        persisted.BlobPath.ShouldBe(file.BlobPath);
        persisted.UploadedByUserId.ShouldBe(userProfile.IdentityId);
    }

    [Test]
    public async Task Handle_WithoutAuthenticatedUser_Throws()
    {
        var (context, schoolClass, file, _) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateGoodsReceiptImportCommandHandler(context, new FakeUser(null));

        var act = async () => await handler.Handle(new CreateGoodsReceiptImportCommand
        {
            ClassId = schoolClass.Id,
            FileMetadataId = file.Id
        }, CancellationToken.None);

        await act.ShouldThrowAsync<ArgumentException>();
    }
}
