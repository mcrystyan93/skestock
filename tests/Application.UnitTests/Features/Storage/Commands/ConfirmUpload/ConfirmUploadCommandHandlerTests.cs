using FluentResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Storage.Commands;
using skestock.Application.Storage.Commands.ConfirmUpload;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.Storage.Models;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Storage.Commands.ConfirmUpload;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for ConfirmUpload handler tests.
/// Unlike the other feature test contexts (which <c>Ignore&lt;FileMetadata&gt;()</c>), this one
/// actually maps <see cref="FileMetadata"/> and ignores everything else, since the handler only
/// touches the FileMetadata set. The CreatedBy/LastModifiedBy user navigations are ignored to
/// avoid EF's UserProfile relationship configuration in the in-memory model.
/// </summary>
public class FileMetadataTestDbContext(DbContextOptions<FileMetadataTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();

        builder.Entity<FileMetadata>(b =>
        {
            b.Ignore(f => f.CreatedBy);
            b.Ignore(f => f.LastModifiedBy);
        });

        builder.Ignore<UserProfile>();
        builder.Ignore<Category>();
        builder.Ignore<Item>();
        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
        builder.Ignore<ClassBalance>();
    }
}

public class ConfirmUploadCommandHandlerTests
{
    private static FileMetadataTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FileMetadataTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FileMetadataTestDbContext(options);
    }

    private static FileMetadata NewPendingFile(Guid fileId) => new()
    {
        FileId = fileId,
        OriginalName = "receipt.pdf",
        BlobContainer = "uploads",
        BlobPath = $"{fileId}/receipt.pdf",
        ContentType = "application/pdf",
        Status = FileStatus.Pending
    };

    [Test]
    public async Task Handle_WhenFileNotInDatabase_ReturnsFileNotFound()
    {
        await using var context = CreateContext();
        var blob = new Mock<IBlobStorageService>();

        var handler = new ConfirmUploadCommandHandler(context, blob.Object);
        var result = await handler.Handle(
            new ConfirmUploadCommand { FileId = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is StorageErrors.FileNotFound);
        blob.Verify(b => b.GetBlobInfoAsync(It.IsAny<GetBlobInfoDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_WhenBlobDoesNotExist_ReturnsBlobNotFoundAndDoesNotComplete()
    {
        await using var context = CreateContext();
        var fileId = Guid.NewGuid();
        context.FileMetadata.Add(NewPendingFile(fileId));
        await context.SaveChangesAsync(CancellationToken.None);

        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.GetBlobInfoAsync(It.IsAny<GetBlobInfoDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlobInfo?)null);

        var handler = new ConfirmUploadCommandHandler(context, blob.Object);
        var result = await handler.Handle(
            new ConfirmUploadCommand { FileId = fileId }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is StorageErrors.BlobNotFound);

        var persisted = await context.FileMetadata.SingleAsync(f => f.FileId == fileId, CancellationToken.None);
        persisted.Status.ShouldBe(FileStatus.Pending);
        persisted.CompletedDate.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WhenBlobExists_UpdatesMetadataAndReturnsCompletedFile()
    {
        await using var context = CreateContext();
        var fileId = Guid.NewGuid();
        var file = NewPendingFile(fileId);
        context.FileMetadata.Add(file);
        await context.SaveChangesAsync(CancellationToken.None);

        var blob = new Mock<IBlobStorageService>();
        GetBlobInfoDto? capturedDto = null;
        blob.Setup(b => b.GetBlobInfoAsync(It.IsAny<GetBlobInfoDto>(), It.IsAny<CancellationToken>()))
            .Callback<GetBlobInfoDto, CancellationToken>((dto, _) => capturedDto = dto)
            .ReturnsAsync(new BlobInfo(4096, "\"0xETAG\"", "application/pdf"));

        var before = DateTimeOffset.UtcNow;
        var handler = new ConfirmUploadCommandHandler(context, blob.Object);
        var result = await handler.Handle(
            new ConfirmUploadCommand { FileId = fileId }, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        result.IsSuccess.ShouldBeTrue();
        result.Value.SizeBytes.ShouldBe(4096);
        result.Value.ETag.ShouldBe("\"0xETAG\"");
        result.Value.Status.ShouldBe(FileStatus.Completed);
        result.Value.CompletedDate.ShouldNotBeNull();
        result.Value.CompletedDate!.Value.ShouldBeInRange(before, after);

        capturedDto.ShouldNotBeNull();
        capturedDto!.ContainerName.ShouldBe(file.BlobContainer);
        capturedDto.BlobPath.ShouldBe(file.BlobPath);

        var persisted = await context.FileMetadata.SingleAsync(f => f.FileId == fileId, CancellationToken.None);
        persisted.Status.ShouldBe(FileStatus.Completed);
        persisted.SizeBytes.ShouldBe(4096);
        persisted.ETag.ShouldBe("\"0xETAG\"");
    }
}
