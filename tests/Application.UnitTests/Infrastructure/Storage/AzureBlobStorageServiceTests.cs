using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Storage.DTOs;
using skestock.Infrastructure.Storage;

namespace skestock.Application.UnitTests.Infrastructure.Storage;

/// <summary>
/// Unit tests for <see cref="AzureBlobStorageService"/>. The Azure SDK client chain
/// (<see cref="BlobServiceClient"/> → <see cref="BlobContainerClient"/> → <see cref="BlobClient"/>)
/// is fully mocked with Moq (every method used is <c>virtual</c>), so no real storage account or
/// Azurite emulator is required — these verify the service's wiring/permission/SAS logic only.
/// </summary>
public class AzureBlobStorageServiceTests
{
    private const string Container = "app-files";
    private const string BlobPath = "2026/09/receipt.pdf";

    private sealed record Harness(
        AzureBlobStorageService Service,
        Mock<BlobServiceClient> ServiceClient,
        Mock<BlobContainerClient> Container,
        Mock<BlobClient> Blob);

    private static Harness CreateHarness()
    {
        var blob = new Mock<BlobClient>();
        var container = new Mock<BlobContainerClient>();
        var serviceClient = new Mock<BlobServiceClient>();

        container.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blob.Object);
        serviceClient.Setup(s => s.GetBlobContainerClient(It.IsAny<string>())).Returns(container.Object);

        return new Harness(new AzureBlobStorageService(serviceClient.Object), serviceClient, container, blob);
    }

    [Test]
    public async Task GenerateUploadSasUriAsync_ResolvesContainerAndBlob_AndReturnsGeneratedUri()
    {
        var harness = CreateHarness();
        var expected = new Uri("https://acct.blob.core.windows.net/app-files/receipt.pdf?sig=upload");
        harness.Blob.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>())).Returns(expected);

        var result = await harness.Service.GenerateUploadSasUriAsync(
            new GenerateUploadSasUriDto(Container, BlobPath, TimeSpan.FromMinutes(10)), CancellationToken.None);

        result.ShouldBe(expected);
        harness.ServiceClient.Verify(s => s.GetBlobContainerClient(Container), Times.Once);
        harness.Container.Verify(c => c.GetBlobClient(BlobPath), Times.Once);
    }

    [Test]
    public async Task GenerateUploadSasUriAsync_BuildsWriteAndCreatePermissionsForRequestedWindow()
    {
        var harness = CreateHarness();
        var validFor = TimeSpan.FromMinutes(15);
        BlobSasBuilder? captured = null;
        harness.Blob.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
            .Callback<BlobSasBuilder>(b => captured = b)
            .Returns(new Uri("https://acct.blob.core.windows.net/x"));

        await harness.Service.GenerateUploadSasUriAsync(
            new GenerateUploadSasUriDto(Container, BlobPath, validFor), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.BlobContainerName.ShouldBe(Container);
        captured.BlobName.ShouldBe(BlobPath);
        captured.Resource.ShouldBe("b");
        captured.Permissions.ShouldContain("w");
        captured.Permissions.ShouldContain("c");
        captured.Permissions.ShouldNotContain("r");
        (captured.ExpiresOn - captured.StartsOn).ShouldBe(validFor, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task GenerateDownloadSasUriAsync_BuildsReadOnlyPermissions_AndReturnsGeneratedUri()
    {
        var harness = CreateHarness();
        var expected = new Uri("https://acct.blob.core.windows.net/app-files/receipt.pdf?sig=download");
        BlobSasBuilder? captured = null;
        harness.Blob.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
            .Callback<BlobSasBuilder>(b => captured = b)
            .Returns(expected);

        var result = await harness.Service.GenerateDownloadSasUriAsync(
            new GenerateDownloadSasUriDto(Container, BlobPath, TimeSpan.FromMinutes(5)), CancellationToken.None);

        result.ShouldBe(expected);
        captured.ShouldNotBeNull();
        captured!.Permissions.ShouldBe("r");
        captured.Resource.ShouldBe("b");
        harness.ServiceClient.Verify(s => s.GetBlobContainerClient(Container), Times.Once);
        harness.Container.Verify(c => c.GetBlobClient(BlobPath), Times.Once);
    }

    [Test]
    public async Task GetBlobInfoAsync_WhenBlobExists_ReturnsMappedProperties()
    {
        var harness = CreateHarness();
        harness.Blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var properties = BlobsModelFactory.BlobProperties(
            contentLength: 2048,
            eTag: new ETag("\"0xETAGVALUE\""),
            contentType: "application/pdf");
        harness.Blob.Setup(b => b.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(properties, Mock.Of<Response>()));

        var info = await harness.Service.GetBlobInfoAsync(
            new GetBlobInfoDto(Container, BlobPath), CancellationToken.None);

        info.ShouldNotBeNull();
        info!.SizeBytes.ShouldBe(2048);
        info.ETag.ShouldBe("\"0xETAGVALUE\"");
        info.ContentType.ShouldBe("application/pdf");
    }

    [Test]
    public async Task GetBlobInfoAsync_WhenBlobDoesNotExist_ReturnsNullWithoutFetchingProperties()
    {
        var harness = CreateHarness();
        harness.Blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var info = await harness.Service.GetBlobInfoAsync(
            new GetBlobInfoDto(Container, BlobPath), CancellationToken.None);

        info.ShouldBeNull();
        harness.Blob.Verify(
            b => b.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ResolvesBlobAndCallsDeleteIfExists()
    {
        var harness = CreateHarness();
        harness.Blob.Setup(b => b.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        await harness.Service.DeleteAsync(new DeleteDto(Container, BlobPath), CancellationToken.None);

        harness.ServiceClient.Verify(s => s.GetBlobContainerClient(Container), Times.Once);
        harness.Container.Verify(c => c.GetBlobClient(BlobPath), Times.Once);
        harness.Blob.Verify(
            b => b.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
