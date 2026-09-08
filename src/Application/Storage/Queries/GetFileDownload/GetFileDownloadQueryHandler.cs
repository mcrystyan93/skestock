using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Storage.Queries.GetFileDownload;

public class GetFileDownloadQueryHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<GetFileDownloadQuery, Result<FileDownloadResult>>
{
    public async ValueTask<Result<FileDownloadResult>> Handle(GetFileDownloadQuery request,
        CancellationToken cancellationToken)
    {
        var file = await dbContext.FileMetadata
            .SingleOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (file is null)
            return Result.Fail(new StorageErrors.FileNotFound(request.Id));

        if (file.Status != FileStatus.Completed)
            return Result.Fail(new StorageErrors.BlobNotFound(request.Id));

        var sasUri = await blobStorageService.GenerateDownloadSasUriAsync(
            new GenerateDownloadSasUriDto(file.BlobContainer, file.BlobPath, TimeSpan.FromMinutes(10)),
            cancellationToken);

        return Result.Ok(new FileDownloadResult(FileMetadataDto.FromEntity(file), sasUri));
    }
}
