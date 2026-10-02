using skestock.Domain.Entities;
using skestock.Domain.Entities.Storage;

namespace skestock.Application.Storage.Commands.ConfirmUpload;

public class ConfirmUploadCommand : IRequest<Result<FileMetadata>>
{
    public Guid FileId { get; init; }
}
