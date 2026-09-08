using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Commands.CreateCategoryImport;
using skestock.Application.Features.Categories.Commands.ProcessCategoryImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Commands.CreateCategoryImport;

public class FakeUser(Guid? id) : IUser
{
    public Guid? Id { get; } = id;
    public List<string>? Roles { get; } = [];
}

public class CreateCategoryImportCommandHandlerTests
{
    private static async Task<(CategoryImportTestDbContext Context, FileMetadata File, UserProfile UserProfile)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<CategoryImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CategoryImportTestDbContext(options);

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "categories.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/2026/categories.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Status = FileStatus.Completed
        };
        context.FileMetadata.Add(file);

        // UploadedByUserId links to the caller's Identity/AspNetUsers id (UserProfile.IdentityId is
        // the FK's principal key - see CategoryImportConfiguration), so the profile only needs to exist.
        var userProfile = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        context.UserProfiles.Add(userProfile);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, file, userProfile);
    }

    [Test]
    public async Task Handle_WithValidData_CreatesProcessingImportAndReturnsDto()
    {
        var (context, file, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateCategoryImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateCategoryImportCommand { FileMetadataId = file.Id };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Status.ShouldBe(CategoryImportStatus.Processing);
        result.Value.FileMetadataId.ShouldBe(file.Id);
        result.Value.BlobPath.ShouldBe(file.BlobPath);
    }

    [Test]
    public async Task Handle_CopiesBlobPathFromFileMetadataAndStampsUploader()
    {
        var (context, file, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateCategoryImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new CreateCategoryImportCommand { FileMetadataId = file.Id }, CancellationToken.None);

        var persisted = await context.CategoryImports.SingleAsync(CancellationToken.None);
        persisted.Id.ShouldBe(result.Value.Id);
        persisted.Status.ShouldBe(CategoryImportStatus.Processing);
        persisted.FileMetadataId.ShouldBe(file.Id);
        persisted.BlobPath.ShouldBe(file.BlobPath);
        persisted.UploadedByUserId.ShouldBe(userProfile.IdentityId);
    }

    [Test]
    public async Task Handle_EnqueuesProcessOutboxMessageOnCategoryImportQueue()
    {
        var (context, file, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateCategoryImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new CreateCategoryImportCommand { FileMetadataId = file.Id }, CancellationToken.None);

        var outbox = await context.OutboxMessages.SingleAsync(CancellationToken.None);
        outbox.QueueName.ShouldBe(skestock.Shared.Services.CategoryImportQueue);
        outbox.Type.ShouldBe(typeof(ProcessCategoryImportCommand).AssemblyQualifiedName);
        outbox.UserId.ShouldBe(userProfile.IdentityId);
        outbox.Payload.ShouldContain(result.Value.Id.ToString());
    }

    [Test]
    public async Task Handle_WithoutAuthenticatedUser_Throws()
    {
        var (context, file, _) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateCategoryImportCommandHandler(context, new FakeUser(null));

        var act = async () => await handler.Handle(new CreateCategoryImportCommand { FileMetadataId = file.Id }, CancellationToken.None);

        await act.ShouldThrowAsync<ArgumentException>();
    }
}
