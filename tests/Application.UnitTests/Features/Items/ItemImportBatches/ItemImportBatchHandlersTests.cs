using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Models.Options;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Features.Items.Commands.ConfirmItemImportBatch;
using skestock.Application.Features.Items.Commands.CreateItemImportBatch;
using skestock.Application.Features.Items.Commands.ProcessItemImportBatch;
using skestock.Application.Features.Items.Queries.GetAllItemImportBatches;
using skestock.Application.Features.Items.Queries.GetItemImportBatchById;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Items.ItemImportBatches;

public class ItemImportBatchHandlersTests
{
    private sealed class TestUser(Guid id) : skestock.Application.Common.Interfaces.IUser
    {
        public Guid? Id { get; } = id;
        public List<string>? Roles { get; } = [];
    }

    private static ItemImportBatchTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ItemImportBatchTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ItemImportBatchTestDbContext(options);
    }

    private static async Task<(ItemImportBatch Batch, UserProfile User, FileMetadata File)> SeedBatchAsync(
        ItemImportBatchTestDbContext context, string fileName = "items.pdf")
    {
        var user = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff" };
        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = fileName,
            BlobContainer = "app-files",
            BlobPath = $"imports/{fileName}",
            ContentType = "application/pdf",
            Status = FileStatus.Completed
        };
        var batch = ItemImportBatch.Create(user.IdentityId, null, [file.Id]);
        batch.UploadedByUser = user;
        batch.Files.Single().FileMetadata = file;
        context.ItemImportBatches.Add(batch);
        await context.SaveChangesAsync(CancellationToken.None);
        return (batch, user, file);
    }

    [Test]
    public async Task Create_WithRepeatedClientRequestId_ReturnsExistingBatchAndOneQueueMessage()
    {
        await using var context = CreateContext();
        var user = new UserProfile { IdentityId = Guid.NewGuid() };
        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "items.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/items.pdf",
            ContentType = "application/pdf"
        };
        context.UserProfiles.Add(user);
        context.FileMetadata.Add(file);
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = new CreateItemImportBatchCommandHandler(context, new TestUser(user.IdentityId));
        var command = new CreateItemImportBatchCommand
        {
            FileMetadataIds = [file.Id],
            ClientRequestId = Guid.NewGuid()
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var repeated = await handler.Handle(command, CancellationToken.None);

        repeated.Value.Id.ShouldBe(first.Value.Id);
        (await context.OutboxMessages.CountAsync(CancellationToken.None)).ShouldBe(1);
    }

    [Test]
    public async Task Create_WithReusedClientRequestIdAndDifferentFiles_ReturnsConflict()
    {
        await using var context = CreateContext();
        var (_, user, file) = await SeedBatchAsync(context);
        var handler = new CreateItemImportBatchCommandHandler(context, new TestUser(user.IdentityId));
        var clientRequestId = Guid.NewGuid();
        await handler.Handle(new CreateItemImportBatchCommand
        {
            ClientRequestId = clientRequestId,
            FileMetadataIds = [file.Id]
        }, CancellationToken.None);

        var result = await handler.Handle(new CreateItemImportBatchCommand
        {
            ClientRequestId = clientRequestId,
            FileMetadataIds = [Guid.NewGuid()]
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Process_WithExtraction_RecordsSuggestionsAndReleasesLease()
    {
        await using var context = CreateContext();
        var (batch, _, _) = await SeedBatchAsync(context);
        var stream = new MemoryStream();
        var blob = new Mock<IBlobStorageService>();
        blob.Setup(service => service.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);
        var extraction = new Mock<IItemDocumentExtractionService>();
        extraction.Setup(service => service.ExtractAsync<ItemExtractionResult>(
                It.IsAny<IReadOnlyList<DocumentExtractionInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemExtractionResult
            {
                Items = [new ExtractedItem { Name = "Pencil", CategoryName = "Stationery" }]
            });
        var handler = new ProcessItemImportBatchCommandHandler(
            context, blob.Object, extraction.Object,
            NullLogger<ProcessItemImportBatchCommandHandler>.Instance,
            Options.Create(new ImportBatchOptions()));

        await handler.Handle(new ProcessItemImportBatchCommand(batch.Id), CancellationToken.None);

        batch.Status.ShouldBe(ItemImportBatchStatus.PendingReview);
        Assert.That(batch.ExtractedDataJson, Does.Contain("Pencil"));
        stream.CanRead.ShouldBeFalse();
    }

    [Test]
    public async Task Process_WithTransientExtractionFailure_ReleasesLeaseForRetry()
    {
        await using var context = CreateContext();
        var (batch, _, _) = await SeedBatchAsync(context);
        var blob = new Mock<IBlobStorageService>();
        blob.Setup(service => service.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());
        var extraction = new Mock<IItemDocumentExtractionService>();
        extraction.Setup(service => service.ExtractAsync<ItemExtractionResult>(
                It.IsAny<IReadOnlyList<DocumentExtractionInput>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientExtractionException("provider unavailable"));
        var handler = new ProcessItemImportBatchCommandHandler(
            context, blob.Object, extraction.Object,
            NullLogger<ProcessItemImportBatchCommandHandler>.Instance,
            Options.Create(new ImportBatchOptions()));

        var act = async () => await handler.Handle(new ProcessItemImportBatchCommand(batch.Id), CancellationToken.None);

        await act.ShouldThrowAsync<TransientExtractionException>();
        batch.ProcessingLeaseUntilUtc.ShouldBeNull();
    }

    [Test]
    public async Task Confirm_WithSelectedItem_ReturnsPersistedCatalogValues()
    {
        await using var context = CreateContext();
        var (batch, user, _) = await SeedBatchAsync(context);
        batch.Status = ItemImportBatchStatus.PendingReview;
        var category = new Category { Name = "Stationery" };
        var item = new Item { Name = "Pencil", Unit = "box", Category = category, CategoryId = category.Id };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = new ConfirmItemImportBatchCommandHandler(context, new TestUser(user.IdentityId));

        var result = await handler.Handle(new ConfirmItemImportBatchCommand
        {
            BatchId = batch.Id,
            Items = [new ConfirmItemImportBatchItem { ItemId = item.Id, Name = "Reviewed name" }]
        }, CancellationToken.None);

        result.Value.Items.Single().Name.ShouldBe("Pencil");
        batch.Status.ShouldBe(ItemImportBatchStatus.Confirmed);
    }

    [Test]
    public async Task GetAll_WithFileNameSearch_ReturnsOnlyMatchingBatch()
    {
        await using var context = CreateContext();
        var (matching, _, _) = await SeedBatchAsync(context, "matching.pdf");
        await SeedBatchAsync(context, "other.pdf");
        var handler = new GetAllItemImportBatchesHandler(context);

        var result = await handler.Handle(
            new GetAllItemImportBatchesQuery { SearchTerm = " matching " }, CancellationToken.None);

        result.Value.Data.Single().Id.ShouldBe(matching.Id);
    }

    [Test]
    public async Task GetById_WithBatchOwnedByUser_ReturnsItsFiles()
    {
        await using var context = CreateContext();
        var (batch, user, file) = await SeedBatchAsync(context);
        var handler = new GetItemImportBatchByIdHandler(context, new TestUser(user.IdentityId));

        var result = await handler.Handle(
            new GetItemImportBatchByIdQuery { Id = batch.Id }, CancellationToken.None);

        result.Value.Files.Single().OriginalName.ShouldBe(file.OriginalName);
    }
}
