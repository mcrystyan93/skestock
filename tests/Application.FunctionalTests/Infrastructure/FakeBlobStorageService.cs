using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Models;

namespace skestock.Application.FunctionalTests.Infrastructure;

/// <summary>
/// In-memory <see cref="IBlobStorageService"/> used by functional tests. TestAppHost stands up
/// SQL Server + Redis only (no Azurite), so the real Azure blob client cannot generate SAS URIs.
/// This fake returns a deterministic download URI derived from the container/blob path so tests
/// can assert the query wiring without any storage backend.
/// </summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public const string BaseUrl = "https://fake.blob.local";

    public Task<Uri> GenerateUploadSasUriAsync(GenerateUploadSasUriDto dto, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"{BaseUrl}/{dto.ContainerName}/{dto.BlobPath}?sig=upload"));

    public Task<Uri> GenerateDownloadSasUriAsync(GenerateDownloadSasUriDto dto, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"{BaseUrl}/{dto.ContainerName}/{dto.BlobPath}?sig=download"));

    public Task<BlobInfo?> GetBlobInfoAsync(GetBlobInfoDto dto, CancellationToken cancellationToken) =>
        Task.FromResult<BlobInfo?>(new BlobInfo(0, "\"0xFAKE\"", "application/octet-stream"));

    public Task DeleteAsync(DeleteDto dto, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<Stream> DownloadAsync(DownloadDto dto, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream());
}
