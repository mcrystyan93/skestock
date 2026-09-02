using skestock.Domain.Entities;

namespace skestock.Application.Storage.Commands.ConfirmUpload;

public class ConfirmUploadCommand : IRequest<Result<FileMetadata>>
{
    public Guid FileId { get; init; }
}
