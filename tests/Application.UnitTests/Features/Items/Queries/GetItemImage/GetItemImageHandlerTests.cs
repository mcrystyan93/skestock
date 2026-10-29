using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Items;
using skestock.Application.Features.Items.Models;
using skestock.Application.Features.Items.Queries.GetItemImage;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Models;
using skestock.Application.UnitTests.Features.Items.Commands.EditItem;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Items.Queries.GetItemImage;

public class GetItemImageHandlerTests
{
    private const string Container = "app-files";
    private const string OriginalPath = "2026/10/a.jpg";

    private sealed record Harness(
        ItemTestDbContext Context, Item Item, Guid FileId, Mock<IBlobStorageService> Blobs, GetItemImageHandler Handler);

    private static async Task<Harness> CreateAsync(
        bool withImage = true, FileStatus status = FileStatus.Completed, bool withMetadata = true)
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ItemTestDbContext(options);
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        var fileId = Guid.NewGuid();
        var item = new Item { Name = "Pencil", Unit = "unit", Category = category, ImageFileId = withImage ? fileId : null };
        context.Items.Add(item);
        if (withMetadata)
        {
            context.FileMetadata.Add(new FileMetadata
            {
                FileId = fileId, OriginalName = "a.jpg", BlobContainer = Container, BlobPath = OriginalPath,
                ContentType = "image/jpeg", SizeBytes = 3, Status = status
            });
        }
        await context.SaveChangesAsync(CancellationToken.None);

        var blobs = new Mock<IBlobStorageService>();
        blobs.Setup(b => b.TryDownloadAsync(new DownloadDto(OriginalPath, Container), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new BlobContent(new MemoryStream([1]), "application/octet-stream"));
        return new Harness(context, item, fileId, blobs, new GetItemImageHandler(context, blobs.Object));
    }

    [Test]
    public async Task Handle_ItemMissing_ReturnsItemNotFound()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;

        var result = await h.Handler.Handle(new GetItemImageQuery(Guid.NewGuid(), ItemImageSize.Full), CancellationToken.None);

        result.Errors[0].ShouldBeOfType<ItemErrors.ItemNotFound>();
    }

    [Test]
    public async Task Handle_ItemWithoutImage_ReturnsImageNotFound()
    {
        var h = await CreateAsync(withImage: false);
        await using var _ = h.Context;

        var result = await h.Handler.Handle(new GetItemImageQuery(h.Item.Id, ItemImageSize.Thumb), CancellationToken.None);

        result.Errors[0].ShouldBeOfType<ItemErrors.ImageNotFound>();
    }

    [TestCase(false, FileStatus.Completed)]
    [TestCase(true, FileStatus.Deleted)]
    public async Task Handle_MetadataMissingOrDeleted_ReturnsImageNotFound(bool withMetadata, FileStatus status)
    {
        var h = await CreateAsync(withMetadata: withMetadata, status: status);
        await using var _ = h.Context;

        var result = await h.Handler.Handle(new GetItemImageQuery(h.Item.Id, ItemImageSize.Full), CancellationToken.None);

        result.Errors[0].ShouldBeOfType<ItemErrors.ImageNotFound>();
    }

    [Test]
    public async Task Handle_Full_ReturnsOriginalWithStoredContentType()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;

        var result = await h.Handler.Handle(new GetItemImageQuery(h.Item.Id, ItemImageSize.Full), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ContentType.ShouldBe("image/jpeg");
        result.Value.IsFallback.ShouldBeFalse();
        h.Blobs.Verify(b => b.TryDownloadAsync(
            new DownloadDto(ItemImagePaths.Thumbnail(h.FileId), Container), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_ThumbExists_ReturnsWebpNotFallback()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;
        h.Blobs.Setup(b => b.TryDownloadAsync(
                new DownloadDto(ItemImagePaths.Thumbnail(h.FileId), Container), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobContent(new MemoryStream([7]), "image/webp"));

        var result = await h.Handler.Handle(new GetItemImageQuery(h.Item.Id, ItemImageSize.Thumb), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ContentType.ShouldBe("image/webp");
        result.Value.IsFallback.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ThumbMissing_FallsBackToOriginal()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;
        h.Blobs.Setup(b => b.TryDownloadAsync(
                new DownloadDto(ItemImagePaths.Thumbnail(h.FileId), Container), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlobContent?)null);

        var result = await h.Handler.Handle(new GetItemImageQuery(h.Item.Id, ItemImageSize.Thumb), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ContentType.ShouldBe("image/jpeg");
        result.Value.IsFallback.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_OriginalBlobMissing_ReturnsImageNotFound()
    {
        var h = await CreateAsync();
        await using var _ = h.Context;
        h.Blobs.Setup(b => b.TryDownloadAsync(new DownloadDto(OriginalPath, Container), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlobContent?)null);

        var result = await h.Handler.Handle(new GetItemImageQuery(h.Item.Id, ItemImageSize.Full), CancellationToken.None);

        result.Errors[0].ShouldBeOfType<ItemErrors.ImageNotFound>();
    }
}
