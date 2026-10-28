using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Application.Features.Items.Commands.EditItem;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Models;
using skestock.Application.Common.Interfaces;
using skestock.Application.UnitTests.Features.Items.Commands.EditItem;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Items.Images;

public class ItemImageValidationTests
{
    private sealed class Fixture
    {
        public required ItemTestDbContext Context { get; init; }
        public required Category Category { get; init; }
        public required Guid UserId { get; init; }
        public Mock<IBlobStorageService> Blobs { get; } = new();
        public Mock<IImageThumbnailService> Thumbnails { get; } = new();
        public IUser User => ItemImageTestSupport.User(UserId);

        public Fixture()
        {
            Blobs.Setup(b => b.TryDownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new BlobContent(new MemoryStream([1, 2, 3]), "image/png"));
            Thumbnails.Setup(t => t.CanDecode(It.IsAny<Stream>())).Returns(true);
        }

        public CreateItemCommandValidator CreateValidator() =>
            new(Context, Blobs.Object, Thumbnails.Object, User);

        public EditItemCommandValidator EditValidator() =>
            new(Context, Blobs.Object, Thumbnails.Object, User);

        public async Task<Guid> AddFileAsync(
            FileStatus status = FileStatus.Completed,
            string contentType = "image/png",
            long size = 1024,
            Guid? ownerId = null)
        {
            var fileId = Guid.NewGuid();
            Context.FileMetadata.Add(new FileMetadata
            {
                FileId = fileId, OriginalName = "p", BlobContainer = "app-files", BlobPath = $"x/{fileId}",
                ContentType = contentType, SizeBytes = size, Status = status, CreatedById = ownerId ?? UserId
            });
            await Context.SaveChangesAsync();
            return fileId;
        }

        public CreateItemCommand Create(Guid? fileId) =>
            new() { Name = "Pencil", Unit = "unit", CategoryId = Category.Id, ImageFileId = fileId };
    }

    private static async Task<Fixture> CreateFixtureAsync()
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ItemTestDbContext(options);
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return new Fixture { Context = context, Category = category, UserId = Guid.NewGuid() };
    }

    private static void ShouldFailWith(FluentValidation.Results.ValidationResult result, string code) =>
        result.Errors.ShouldContain(e => e.PropertyName == "ImageFileId" && e.ErrorCode == code);

    [Test]
    public async Task NullImage_IsValid_AndNothingIsDownloaded()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;

        var result = await f.CreateValidator().ValidateAsync(f.Create(null));

        result.IsValid.ShouldBeTrue();
        f.Blobs.Verify(b => b.TryDownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ValidImage_IsValid()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        var fileId = await f.AddFileAsync();

        var result = await f.CreateValidator().ValidateAsync(f.Create(fileId));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task UnknownFile_FailsWithInvalidReference()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;

        var result = await f.CreateValidator().ValidateAsync(f.Create(Guid.NewGuid()));

        ShouldFailWith(result, ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task PendingFile_FailsWithInvalidReference()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        var fileId = await f.AddFileAsync(status: FileStatus.Pending);

        var result = await f.CreateValidator().ValidateAsync(f.Create(fileId));

        ShouldFailWith(result, ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task FileOwnedByAnotherUser_FailsWithFileNotOwned()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        var fileId = await f.AddFileAsync(ownerId: Guid.NewGuid());

        var result = await f.CreateValidator().ValidateAsync(f.Create(fileId));

        ShouldFailWith(result, ValidationErrorCodes.FileNotOwned);
    }

    [Test]
    public async Task FileOverFiveMegabytes_FailsWithFileTooLarge()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        var fileId = await f.AddFileAsync(size: 5 * 1024 * 1024 + 1);

        var result = await f.CreateValidator().ValidateAsync(f.Create(fileId));

        ShouldFailWith(result, ValidationErrorCodes.FileTooLarge);
    }

    [Test]
    public async Task ExactlyFiveMegabytes_IsValid()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        var fileId = await f.AddFileAsync(size: 5 * 1024 * 1024);

        var result = await f.CreateValidator().ValidateAsync(f.Create(fileId));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task PdfFile_FailsWithUnsupportedFileType()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        var fileId = await f.AddFileAsync(contentType: "application/pdf");

        var result = await f.CreateValidator().ValidateAsync(f.Create(fileId));

        ShouldFailWith(result, ValidationErrorCodes.UnsupportedFileType);
    }

    [Test]
    public async Task FileUsedByAnotherItem_FailsWithDuplicateFile()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        var fileId = await f.AddFileAsync();
        f.Context.Items.Add(new Item { Name = "Other", Unit = "unit", CategoryId = f.Category.Id, ImageFileId = fileId });
        await f.Context.SaveChangesAsync();

        var result = await f.CreateValidator().ValidateAsync(f.Create(fileId));

        ShouldFailWith(result, ValidationErrorCodes.DuplicateFile);
    }

    [Test]
    public async Task UndecodableFile_FailsWithInvalidImage()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        var fileId = await f.AddFileAsync();
        f.Thumbnails.Setup(t => t.CanDecode(It.IsAny<Stream>())).Returns(false);

        var result = await f.CreateValidator().ValidateAsync(f.Create(fileId));

        ShouldFailWith(result, ValidationErrorCodes.InvalidImage);
    }

    [Test]
    public async Task DatabaseCheckFailure_SkipsTheBlobDownload()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        var fileId = await f.AddFileAsync(contentType: "application/pdf");

        await f.CreateValidator().ValidateAsync(f.Create(fileId));

        f.Blobs.Verify(b => b.TryDownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Edit_KeepingTheItemsCurrentImage_SkipsAllChecks()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        // Another user's file: ownership must not block an unrelated edit of an item that already has it.
        var fileId = await f.AddFileAsync(ownerId: Guid.NewGuid());
        var item = new Item { Name = "Pencil", Unit = "unit", CategoryId = f.Category.Id, ImageFileId = fileId };
        f.Context.Items.Add(item);
        await f.Context.SaveChangesAsync();

        var result = await f.EditValidator().ValidateAsync(new EditItemCommand
        {
            Id = item.Id, Name = "Pencil", Unit = "unit", CategoryId = f.Category.Id, ImageFileId = fileId
        });

        result.IsValid.ShouldBeTrue();
        f.Blobs.Verify(b => b.TryDownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Edit_ReplacingWithNewFile_RunsChecks()
    {
        var f = await CreateFixtureAsync();
        await using var _ = f.Context;
        var item = new Item { Name = "Pencil", Unit = "unit", CategoryId = f.Category.Id, ImageFileId = Guid.NewGuid() };
        f.Context.Items.Add(item);
        var newFile = await f.AddFileAsync(contentType: "application/pdf");

        var result = await f.EditValidator().ValidateAsync(new EditItemCommand
        {
            Id = item.Id, Name = "Pencil", Unit = "unit", CategoryId = f.Category.Id, ImageFileId = newFile
        });

        ShouldFailWith(result, ValidationErrorCodes.UnsupportedFileType);
    }
}
