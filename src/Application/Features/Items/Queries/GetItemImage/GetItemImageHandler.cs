using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Queries.GetItemImage;

public class GetItemImageHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorage)
    : IRequestHandler<GetItemImageQuery, Result<ItemImageResult>>
{
    private const string ThumbnailContentType = "image/webp";

    public async ValueTask<Result<ItemImageResult>> Handle(GetItemImageQuery request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.Id == request.ItemId)
            .Select(i => new { i.ImageFileId })
            .SingleOrDefaultAsync(cancellationToken);

        if (item is null)
            return Result.Fail(new ItemErrors.ItemNotFound(request.ItemId));

        if (item.ImageFileId is not { } imageFileId)
            return Result.Fail(new ItemErrors.ImageNotFound(request.ItemId));

        var file = await dbContext.FileMetadata
            .AsNoTracking()
            .Where(f => f.FileId == imageFileId && f.Status == FileStatus.Completed)
            .Select(f => new { f.BlobContainer, f.BlobPath, f.ContentType })
            .SingleOrDefaultAsync(cancellationToken);

        if (file is null)
            return Result.Fail(new ItemErrors.ImageNotFound(request.ItemId));

        if (request.Size == ItemImageSize.Thumb)
        {
            var thumbnail = await blobStorage.TryDownloadAsync(
                new DownloadDto(ItemImagePaths.Thumbnail(imageFileId), file.BlobContainer), cancellationToken);

            if (thumbnail is not null)
                return Result.Ok(new ItemImageResult(thumbnail.Content, ThumbnailContentType, IsFallback: false));
        }

        var original = await blobStorage.TryDownloadAsync(
            new DownloadDto(file.BlobPath, file.BlobContainer), cancellationToken);

        if (original is null)
            return Result.Fail(new ItemErrors.ImageNotFound(request.ItemId));

        // Metadata content type was allow-list validated; the blob header is whatever the client PUT.
        return Result.Ok(new ItemImageResult(
            original.Content, file.ContentType, IsFallback: request.Size == ItemImageSize.Thumb));
    }
}
