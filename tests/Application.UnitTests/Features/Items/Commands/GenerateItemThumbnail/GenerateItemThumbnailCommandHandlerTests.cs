using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items;
using skestock.Application.UnitTests.Features.Items.Commands.EditItem;
using skestock.Application.Features.Items.Commands.GenerateItemThumbnail;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Models;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Items.Commands.GenerateItemThumbnail;

public class GenerateItemThumbnailCommandHandlerTests
{
    private const string Container = "app-files";

    private sealed record Harness(
        ItemTestDbContext Context,
        Item Item,
        Guid FileId,
        Mock<IBlobStorageService> Blobs,
        Mock<IImageThumbnailService> Thumbnails,
        GenerateItemThumbnailCommandHandler Handler);

    private static async Task<Harness> CreateAsync(bool withMetadata = true)
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ItemTestDbContext(options);
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        var fileId = Guid.NewGuid();
        var item = new Item { Name = "Pencil", Unit = "unit", Category = category, ImageFileId = fileId };
        context.Items.Add(item);
        if (withMetadata)
        {
            context.FileMetadata.Add(new FileMetadata
            {
                FileId = fileId, OriginalName = "p.png", BlobContainer = Container,
                BlobPath = "2026/10/p.png", ContentType = "image/png", SizeBytes = 3, Status = FileStatus.Completed
            });
        }
        await context.SaveChangesAsync(CancellationToken.None);

        var blobs = new Mock<IBlobStorageService>();
        blobs.Setup(b => b.TryDownloadAsync(
                new DownloadDto("2026/10/p.png", Container), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new BlobContent(new MemoryStream([1, 2, 3]), "image/png"));
        var thumbnails = new Mock<IImageThumbnailService>();
        thumbnails.Setup(t => t.CreateThumbnail(It.IsAny<Stream>())).Returns([9, 9]);

        return new Harness(context, item, fileId, blobs, thumbnails,
            new GenerateItemThumbnailCommandHandler(context, blobs.Object, thumbnails.Object));
    }

    private static void VerifyNoBlobWrite(Harness h) =>
        h.Blobs.Verify(b => b.UploadAsync(It.IsAny<UploadDto>(), It.IsAny<CancellationToken>()), Times.Never);

    [Test]
    public async Task Handle_UploadsWebpThumbnailAtFixedPath()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;
        UploadDto? uploaded = null;
        h.Blobs.Setup(b => b.UploadAsync(It.IsAny<UploadDto>(), It.IsAny<CancellationToken>()))
            .Callback<UploadDto, CancellationToken>((dto, _) => uploaded = dto)
            .Returns(Task.CompletedTask);

        var result = await h.Handler.Handle(new GenerateItemThumbnailCommand(h.Item.Id, h.FileId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        uploaded.ShouldNotBeNull();
        uploaded!.ContainerName.ShouldBe(Container);
        uploaded.BlobPath.ShouldBe(ItemImagePaths.Thumbnail(h.FileId));
        uploaded.BlobPath.ShouldBe($"thumbnails/{h.FileId}.webp");
        uploaded.ContentType.ShouldBe("image/webp");
    }

    [Test]
    public async Task Handle_StaleImageFileId_IsNoOp()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;

        var result = await h.Handler.Handle(new GenerateItemThumbnailCommand(h.Item.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        h.Blobs.Verify(b => b.TryDownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoBlobWrite(h);
    }

    [Test]
    public async Task Handle_ItemMissing_IsNoOp()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;

        var result = await h.Handler.Handle(new GenerateItemThumbnailCommand(Guid.NewGuid(), h.FileId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        VerifyNoBlobWrite(h);
    }

    [Test]
    public async Task Handle_MetadataMissing_IsNoOp()
    {
        var h = await CreateAsync(withMetadata: false);
        await using var _ = h.Context;

        var result = await h.Handler.Handle(new GenerateItemThumbnailCommand(h.Item.Id, h.FileId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        VerifyNoBlobWrite(h);
    }

    [Test]
    public async Task Handle_OriginalBlobMissing_IsNoOp()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;
        h.Blobs.Setup(b => b.TryDownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlobContent?)null);

        var result = await h.Handler.Handle(new GenerateItemThumbnailCommand(h.Item.Id, h.FileId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        VerifyNoBlobWrite(h);
    }

    [Test]
    public async Task Handle_CorruptImage_FailsWithInvalidImage()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;
        h.Thumbnails.Setup(t => t.CreateThumbnail(It.IsAny<Stream>())).Throws(new InvalidDataException());

        var result = await h.Handler.Handle(new GenerateItemThumbnailCommand(h.Item.Id, h.FileId), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors[0].ShouldBeOfType<ItemErrors.InvalidImage>();
        VerifyNoBlobWrite(h);
    }

    [Test]
    public async Task Handle_DuplicateDelivery_SucceedsTwiceWithSamePath()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;
        var paths = new List<string>();
        h.Blobs.Setup(b => b.UploadAsync(It.IsAny<UploadDto>(), It.IsAny<CancellationToken>()))
            .Callback<UploadDto, CancellationToken>((dto, _) => paths.Add(dto.BlobPath))
            .Returns(Task.CompletedTask);
        var command = new GenerateItemThumbnailCommand(h.Item.Id, h.FileId);

        (await h.Handler.Handle(command, CancellationToken.None)).IsSuccess.ShouldBeTrue();
        (await h.Handler.Handle(command, CancellationToken.None)).IsSuccess.ShouldBeTrue();

        paths.Count.ShouldBe(2);
        paths.Distinct().Count().ShouldBe(1);
    }
}
