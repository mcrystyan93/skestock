using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Commands.CreateItemImport;
using skestock.Application.Features.Items.Commands.ProcessItemImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Commands.CreateItemImport;

public class FakeUser(Guid? id) : IUser
{
    public Guid? Id { get; } = id;
    public List<string>? Roles { get; } = [];
}

public class CreateItemImportCommandHandlerTests
{
    private static async Task<(ItemImportTestDbContext Context, FileMetadata File, UserProfile UserProfile)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ItemImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ItemImportTestDbContext(options);

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

        // UploadedByUserId links to the caller's Identity/AspNetUsers id (UserProfile.IdentityId is
        // the FK's principal key - see ItemImportConfiguration), so the profile only needs to exist.
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
        var handler = new CreateItemImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateItemImportCommand { FileMetadataId = file.Id };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Status.ShouldBe(ItemImportStatus.Processing);
        result.Value.FileMetadataId.ShouldBe(file.Id);
        result.Value.BlobPath.ShouldBe(file.BlobPath);
    }

    [Test]
    public async Task Handle_CopiesBlobPathFromFileMetadataAndStampsUploader()
    {
        var (context, file, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new CreateItemImportCommand { FileMetadataId = file.Id }, CancellationToken.None);

        var persisted = await context.ItemImports.SingleAsync(CancellationToken.None);
        persisted.Id.ShouldBe(result.Value.Id);
        persisted.Status.ShouldBe(ItemImportStatus.Processing);
        persisted.FileMetadataId.ShouldBe(file.Id);
        persisted.BlobPath.ShouldBe(file.BlobPath);
        persisted.UploadedByUserId.ShouldBe(userProfile.IdentityId);
    }

    [Test]
    public async Task Handle_EnqueuesProcessOutboxMessageOnItemImportQueue()
    {
        var (context, file, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemImportCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new CreateItemImportCommand { FileMetadataId = file.Id }, CancellationToken.None);

        var outbox = await context.OutboxMessages.SingleAsync(CancellationToken.None);
        outbox.QueueName.ShouldBe(skestock.Shared.Services.ItemImportQueue);
        outbox.Type.ShouldBe(typeof(ProcessItemImportCommand).AssemblyQualifiedName);
        outbox.UserId.ShouldBe(userProfile.IdentityId);
        outbox.Payload.ShouldContain(result.Value.Id.ToString());
    }

    [Test]
    public async Task Handle_WithoutAuthenticatedUser_Throws()
    {
        var (context, file, _) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemImportCommandHandler(context, new FakeUser(null));

        var act = async () => await handler.Handle(new CreateItemImportCommand { FileMetadataId = file.Id }, CancellationToken.None);

        await act.ShouldThrowAsync<ArgumentException>();
    }
}
