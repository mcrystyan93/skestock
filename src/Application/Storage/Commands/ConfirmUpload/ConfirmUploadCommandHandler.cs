using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Storage.Commands.ConfirmUpload;

public class ConfirmUploadCommandHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<ConfirmUploadCommand, Result<FileMetadata>>
{
    public async ValueTask<Result<FileMetadata>> Handle(ConfirmUploadCommand request,
        CancellationToken cancellationToken)
    {
        var file = await dbContext.FileMetadata
            .SingleOrDefaultAsync(f => f.FileId == request.FileId, cancellationToken);

        if (file is null)
            return Result.Fail(new StorageErrors.FileNotFound(request.FileId));

        var blobInfo = await blobStorageService.GetBlobInfoAsync(
            new GetBlobInfoDto(file.BlobContainer, file.BlobPath), cancellationToken);

        if (blobInfo is null)
            return Result.Fail(new StorageErrors.BlobNotFound(request.FileId));

        file.SizeBytes = blobInfo.SizeBytes;
        file.ETag = blobInfo.ETag;
        file.Status = FileStatus.Completed;
        file.CompletedDate = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(file);
    }
}
