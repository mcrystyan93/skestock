using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items;

internal static class ItemImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    // itemId is the item being edited (null on create). The cheap database checks run first; the
    // blob download for the decode check only happens when they all pass.
    public static async Task ValidateAsync<T>(
        IApplicationDbContext dbContext,
        IBlobStorageService blobStorage,
        IImageThumbnailService thumbnailService,
        IUser user,
        Guid? itemId,
        Guid imageFileId,
        ValidationContext<T> context,
        CancellationToken cancellationToken)
    {
        // An edit that keeps the item's current image must not re-run checks (the file may belong to another user).
        if (itemId is { } id &&
            await dbContext.Items.AsNoTracking().AnyAsync(i => i.Id == id && i.ImageFileId == imageFileId, cancellationToken))
            return;

        var file = await dbContext.FileMetadata
            .AsNoTracking()
            .Where(f => f.FileId == imageFileId)
            .Select(f => new { f.Status, f.ContentType, f.SizeBytes, f.CreatedById, f.BlobContainer, f.BlobPath })
            .SingleOrDefaultAsync(cancellationToken);

        if (file is null || file.Status != FileStatus.Completed)
        {
            Fail(context, ValidationErrorCodes.InvalidReference, "The image file does not exist or has not completed upload.");
            return;
        }

        var failed = false;

        if (file.CreatedById != user.Id)
            failed = Fail(context, ValidationErrorCodes.FileNotOwned, "The image file is not owned by the current user.");

        if (!AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            failed = Fail(context, ValidationErrorCodes.UnsupportedFileType, "The image must be a JPEG, PNG, WebP or GIF file.");

        if (file.SizeBytes > MaxBytes)
            failed = Fail(context, ValidationErrorCodes.FileTooLarge, $"The image exceeds the maximum size of {MaxBytes} bytes.");

        if (await dbContext.Items.AsNoTracking().AnyAsync(i => i.ImageFileId == imageFileId && i.Id != itemId, cancellationToken))
            failed = Fail(context, ValidationErrorCodes.DuplicateFile, "The image file is already used by another item.");

        if (failed)
            return;

        var blob = await blobStorage.TryDownloadAsync(new DownloadDto(file.BlobPath, file.BlobContainer), cancellationToken);
        if (blob is null)
        {
            Fail(context, ValidationErrorCodes.InvalidReference, "The image file content was not found.");
            return;
        }

        await using (blob.Content)
        {
            using var buffer = new MemoryStream();
            await blob.Content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            if (!thumbnailService.CanDecode(buffer))
                Fail(context, ValidationErrorCodes.InvalidImage, "The file is not a valid image.");
        }
    }

    private static bool Fail<T>(ValidationContext<T> context, string code, string message)
    {
        context.AddFailure(new ValidationFailure("ImageFileId", message) { ErrorCode = code });
        return true;
    }
}
