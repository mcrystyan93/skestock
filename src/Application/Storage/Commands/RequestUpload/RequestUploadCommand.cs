using skestock.Application.Storage.Models;

namespace skestock.Application.Storage.Commands.RequestUpload;

public class RequestUploadCommand: IRequest<Result<UploadRequestResult>>
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
}
