using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Commands.CreateCategoryImportBatch;
using skestock.Application.Features.Categories.Commands.ProcessCategoryImportBatch;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Commands.CreateCategoryImportBatch;

public class FakeUser(Guid? id) : IUser
{
    public Guid? Id { get; } = id;
    public List<string>? Roles { get; } = [];
}

public class CreateCategoryImportBatchCommandHandlerTests
{
    private static async Task<(CategoryImportBatchTestDbContext Context, FileMetadata File, UserProfile UserProfile)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<CategoryImportBatchTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CategoryImportBatchTestDbContext(options);

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
        // the FK's principal key - see CategoryImportBatchConfiguration), so the profile only needs to exist.
        var userProfile = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        context.UserProfiles.Add(userProfile);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, file, userProfile);
    }

    [Test]
    public async Task Handle_WithValidData_CreatesProcessingImportAndReturnsMutationDto()
    {
        var (context, file, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateCategoryImportBatchCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var command = new CreateCategoryImportBatchCommand { FileMetadataIds = [file.Id] };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Status.ShouldBe(CategoryImportBatchStatus.Processing);
    }

    [Test]
    public async Task Handle_CopiesBlobPathFromFileMetadataAndStampsUploader()
    {
        var (context, file, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateCategoryImportBatchCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new CreateCategoryImportBatchCommand { FileMetadataIds = [file.Id] }, CancellationToken.None);

        var persisted = await context.CategoryImportBatches.SingleAsync(CancellationToken.None);
        persisted.Id.ShouldBe(result.Value.Id);
        persisted.Status.ShouldBe(CategoryImportBatchStatus.Processing);
        persisted.Files.Single().FileMetadataId.ShouldBe(file.Id);
        persisted.UploadedByUserId.ShouldBe(userProfile.IdentityId);
    }

    [Test]
    public async Task Handle_EnqueuesProcessOutboxMessageOnCategoryImportQueue()
    {
        var (context, file, userProfile) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateCategoryImportBatchCommandHandler(context, new FakeUser(userProfile.IdentityId));

        var result = await handler.Handle(new CreateCategoryImportBatchCommand { FileMetadataIds = [file.Id] }, CancellationToken.None);

        var outbox = await context.OutboxMessages.SingleAsync(CancellationToken.None);
        outbox.QueueName.ShouldBe(skestock.Shared.Services.CategoryImportQueue);
        outbox.Type.ShouldBe(typeof(ProcessCategoryImportBatchCommand).AssemblyQualifiedName);
        outbox.UserId.ShouldBe(userProfile.IdentityId);
        outbox.Payload.ShouldContain(result.Value.Id.ToString());
    }

    [Test]
    public async Task Handle_WithoutAuthenticatedUser_Throws()
    {
        var (context, file, _) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateCategoryImportBatchCommandHandler(context, new FakeUser(null));

        var act = async () => await handler.Handle(new CreateCategoryImportBatchCommand { FileMetadataIds = [file.Id] }, CancellationToken.None);

        await act.ShouldThrowAsync<ArgumentException>();
    }
}
