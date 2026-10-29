using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<(string Container, string Path), (byte[] Bytes, string ContentType)> _blobs = new();

    public const string BaseUrl = "https://fake.blob.local";

    public Task<Uri> GenerateUploadSasUriAsync(GenerateUploadSasUriDto dto, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"{BaseUrl}/{dto.ContainerName}/{dto.BlobPath}?sig=upload"));

    public Task<Uri> GenerateDownloadSasUriAsync(GenerateDownloadSasUriDto dto, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"{BaseUrl}/{dto.ContainerName}/{dto.BlobPath}?sig=download"));

    public Task<BlobInfo?> GetBlobInfoAsync(GetBlobInfoDto dto, CancellationToken cancellationToken) =>
        Task.FromResult<BlobInfo?>(new BlobInfo(0, "\"0xFAKE\"", "application/octet-stream"));

    public Task DeleteAsync(DeleteDto dto, CancellationToken cancellationToken)
    {
        _blobs.TryRemove((dto.ContainerName, dto.BlobPath), out _);
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(DownloadDto dto, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(_blobs.TryGetValue((dto.ContainerName, dto.BlobPath), out var blob)
            ? new MemoryStream(blob.Bytes)
            : new MemoryStream());

    public async Task UploadAsync(UploadDto dto, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await dto.Content.CopyToAsync(buffer, cancellationToken);
        _blobs[(dto.ContainerName, dto.BlobPath)] = (buffer.ToArray(), dto.ContentType);
    }

    public Task<BlobContent?> TryDownloadAsync(DownloadDto dto, CancellationToken cancellationToken) =>
        Task.FromResult(_blobs.TryGetValue((dto.ContainerName, dto.BlobPath), out var blob)
            ? new BlobContent(new MemoryStream(blob.Bytes), blob.ContentType)
            : null);

    public bool Exists(string container, string path) => _blobs.ContainsKey((container, path));
}
