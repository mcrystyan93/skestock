using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Models;

namespace skestock.Application.Storage.Interfaces;

public interface IBlobStorageService
{
    Task<Uri> GenerateUploadSasUriAsync(GenerateUploadSasUriDto dto, CancellationToken cancellationToken);
    Task<Uri> GenerateDownloadSasUriAsync(GenerateDownloadSasUriDto dto, CancellationToken cancellationToken);
    Task<BlobInfo?> GetBlobInfoAsync(GetBlobInfoDto dto, CancellationToken cancellationToken);
    Task DeleteAsync(DeleteDto dto, CancellationToken cancellationToken);
    Task<Stream> DownloadAsync(DownloadDto dto, CancellationToken cancellationToken);
}
