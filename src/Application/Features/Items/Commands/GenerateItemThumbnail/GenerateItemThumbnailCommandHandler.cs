using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;

namespace skestock.Application.Features.Items.Commands.GenerateItemThumbnail;

// Delivery is at-least-once and the output path is fixed per image file, so a repeat just overwrites
// the same blob. A stale job (item gone or image since replaced) is a no-op, not a poison message.
public class GenerateItemThumbnailCommandHandler(
    IApplicationDbContext dbContext,
    IBlobStorageService blobStorage,
    IImageThumbnailService thumbnailService)
    : IRequestHandler<GenerateItemThumbnailCommand, Result>
{
    private const string ThumbnailContentType = "image/webp";

    public async ValueTask<Result> Handle(GenerateItemThumbnailCommand request, CancellationToken cancellationToken)
    {
        var currentImageFileId = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.Id == request.ItemId)
            .Select(i => i.ImageFileId)
            .SingleOrDefaultAsync(cancellationToken);

        if (currentImageFileId != request.ImageFileId)
            return Result.Ok();

        var file = await dbContext.FileMetadata
            .AsNoTracking()
            .Where(f => f.FileId == request.ImageFileId)
            .Select(f => new { f.BlobContainer, f.BlobPath })
            .SingleOrDefaultAsync(cancellationToken);

        if (file is null)
            return Result.Ok();

        var original = await blobStorage.TryDownloadAsync(
            new DownloadDto(file.BlobPath, file.BlobContainer), cancellationToken);

        if (original is null)
            return Result.Ok();

        byte[] thumbnail;
        await using (original.Content)
        {
            // Azure streams are forward-only; the decoder needs a seekable one.
            using var buffer = new MemoryStream();
            await original.Content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            try
            {
                thumbnail = thumbnailService.CreateThumbnail(buffer);
            }
            catch (InvalidDataException)
            {
                return Result.Fail(new ItemErrors.InvalidImage(request.ImageFileId));
            }
        }

        using var content = new MemoryStream(thumbnail);
        await blobStorage.UploadAsync(
            new UploadDto(file.BlobContainer, ItemImagePaths.Thumbnail(request.ImageFileId), content, ThumbnailContentType),
            cancellationToken);

        return Result.Ok();
    }
}
