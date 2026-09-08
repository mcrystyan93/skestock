using skestock.Application.Storage.Models;

namespace skestock.Application.Storage.Queries.GetFileDownload;

public class GetFileDownloadQuery : IRequest<Result<FileDownloadResult>>
{
    public Guid Id { get; init; }
}
