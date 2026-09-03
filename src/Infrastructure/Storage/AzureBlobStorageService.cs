using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Models;

namespace skestock.Infrastructure.Storage;

public class AzureBlobStorageService(BlobServiceClient blobServiceClient) : IBlobStorageService
{
    public Task<Uri> GenerateUploadSasUriAsync(GenerateUploadSasUriDto dto, CancellationToken cancellationToken)
    {
        var blob = blobServiceClient.GetBlobContainerClient(dto.ContainerName).GetBlobClient(dto.BlobPath);
        var sas = new BlobSasBuilder
        {
            BlobContainerName = dto.ContainerName,
            BlobName = dto.BlobPath,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.Add(dto.ValidFor)
        };

        sas.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        return Task.FromResult(blob.GenerateSasUri(sas));
    }

    public Task<Uri> GenerateDownloadSasUriAsync(GenerateDownloadSasUriDto dto, CancellationToken cancellationToken)
    {
        var blob = blobServiceClient.GetBlobContainerClient(dto.ContainerName).GetBlobClient(dto.BlobPath);
        var sas = new BlobSasBuilder
        {
            BlobContainerName = dto.ContainerName,
            BlobName = dto.BlobPath,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.Add(dto.ValidFor)
        };

        sas.SetPermissions(BlobSasPermissions.Read);

        return Task.FromResult(blob.GenerateSasUri(sas));
    }

    public async Task<BlobInfo?> GetBlobInfoAsync(GetBlobInfoDto dto, CancellationToken cancellationToken)
    {
        var blob = blobServiceClient.GetBlobContainerClient(dto.ContainerName).GetBlobClient(dto.BlobPath);
        if (!await blob.ExistsAsync(cancellationToken))
            return null;

        var props = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);

        return new BlobInfo(props.Value.ContentLength, props.Value.ETag.ToString(), props.Value.ContentType);
    }

    public async Task DeleteAsync(DeleteDto dto, CancellationToken cancellationToken)
    {
        var blob = blobServiceClient.GetBlobContainerClient(dto.ContainerName).GetBlobClient(dto.BlobPath);

        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<Stream> DownloadAsync(DownloadDto dto, CancellationToken cancellationToken)
    {
        var blob = blobServiceClient.GetBlobContainerClient(dto.ContainerName).GetBlobClient(dto.BlobPath);

        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }
}
