using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Models;

namespace skestock.Infrastructure.Storage;

public class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    IConfiguration? configuration = null) : IBlobStorageService
{
    private readonly Uri? publicBlobEndpoint = CreatePublicBlobEndpoint(configuration);

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

        return Task.FromResult(ToPublicBlobUri(blob.GenerateSasUri(sas)));
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

        return Task.FromResult(ToPublicBlobUri(blob.GenerateSasUri(sas)));
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

    private Uri ToPublicBlobUri(Uri sasUri)
    {
        if (publicBlobEndpoint is null)
        {
            return sasUri;
        }

        var builder = new UriBuilder(publicBlobEndpoint)
        {
            Path = sasUri.AbsolutePath,
            Query = sasUri.Query
        };

        return builder.Uri;
    }

    private static Uri? CreatePublicBlobEndpoint(IConfiguration? configuration)
    {
        var endpoint = configuration?["Storage:PublicBlobEndpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return null;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException(
                "Storage:PublicBlobEndpoint must be an absolute HTTP or HTTPS URI.");
        }

        return uri;
    }
}
