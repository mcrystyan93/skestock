using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceipts;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Queries.GetAllGoodsReceipts;

public class GetAllGoodsReceiptsHandlerTests
{
    [Test]
    public async Task Handle_WithConfirmedImport_ReturnsSourceFileMetadataId()
    {
        await using var context = NewContext();
        var (receipt, file) = AddReceipt(context);

        context.GoodsReceiptImports.Add(new GoodsReceiptImport
        {
            Id = Guid.NewGuid(),
            ClassId = receipt.ClassId,
            FileMetadataId = file.Id,
            FileMetadata = file,
            BlobPath = file.BlobPath,
            UploadedByUserId = Guid.NewGuid(),
            Status = GoodsReceiptImportStatus.Confirmed,
            ResultingGoodsReceiptId = receipt.Id,
            ResultingGoodsReceipt = receipt,
            UploadedAt = receipt.ReceivedAt,
            CreatedDate = receipt.CreatedDate,
            LastModifiedDate = receipt.CreatedDate
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(context).Handle(new GetAllGoodsReceiptsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Single().FileMetadataId.ShouldBe(file.Id);
    }

    [Test]
    public async Task Handle_WithoutImport_ReturnsNullSourceFileMetadataId()
    {
        await using var context = NewContext();
        AddReceipt(context);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(context).Handle(new GetAllGoodsReceiptsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Single().FileMetadataId.ShouldBeNull();
    }

    private static GoodsReceiptListTestDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptListTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GoodsReceiptListTestDbContext(options);
    }

    private static (GoodsReceipt Receipt, FileMetadata File) AddReceipt(GoodsReceiptListTestDbContext context)
    {
        var schoolClass = new SchoolClass
        {
            Id = Guid.NewGuid(),
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        var receipt = new GoodsReceipt
        {
            Id = Guid.NewGuid(),
            ClassId = schoolClass.Id,
            Class = schoolClass,
            Note = "Imported receipt",
            ReceivedAt = new DateTime(2026, 9, 15),
            CreatedDate = new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero),
            LastModifiedDate = new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero)
        };
        var file = new FileMetadata
        {
            Id = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            OriginalName = "receipt.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/2026/receipt.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Status = FileStatus.Completed
        };

        context.SchoolClasses.Add(schoolClass);
        context.GoodsReceipts.Add(receipt);
        context.FileMetadata.Add(file);
        return (receipt, file);
    }

    private static GetAllGoodsReceiptsHandler CreateHandler(IApplicationDbContext context) => new(context);
}
