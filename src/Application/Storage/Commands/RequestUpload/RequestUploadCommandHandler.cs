using skestock.Application.Common.Interfaces;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Storage.Commands.RequestUpload;

public class RequestUploadCommandHandler(IApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    : IRequestHandler<RequestUploadCommand, Result<UploadRequestResult>>
{
    public async ValueTask<Result<UploadRequestResult>> Handle(RequestUploadCommand request,
        CancellationToken cancellationToken)
    {
        var fileId = Guid.NewGuid();
        var safeName = Path.GetFileName(request.FileName);
        var blobPath = $"{fileId}/{safeName}";

        var fileMetadata = new FileMetadata
        {
            FileId = fileId,
            OriginalName = request.FileName,
            BlobContainer = "app-files",
            BlobPath = blobPath,
            ContentType = request.ContentType,
            Status = FileStatus.Pending
        };

        dbContext.FileMetadata.Add(fileMetadata);
        await dbContext.SaveChangesAsync(cancellationToken);

        var sasUri =
            await blobStorageService.GenerateUploadSasUriAsync(new("app-files", blobPath, TimeSpan.FromMinutes(10)),
                cancellationToken);

        return Result.Ok(new UploadRequestResult(fileId, sasUri));
    }
}
