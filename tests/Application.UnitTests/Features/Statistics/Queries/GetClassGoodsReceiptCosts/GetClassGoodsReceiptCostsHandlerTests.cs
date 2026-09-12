using Microsoft.EntityFrameworkCore;
using skestock.Application.Features.Statistics.Queries.GetClassGoodsReceiptCosts;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetClassGoodsReceiptCosts;

public class GetClassGoodsReceiptCostsHandlerTests
{
    private static GoodsReceiptTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new GoodsReceiptTestDbContext(options);
    }

    private static SchoolClass CreateClass(string name = "Fall 2026") => new()
    {
        Name = name,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31)
    };

    private static GoodsReceipt CreateReceipt(
        SchoolClass schoolClass,
        DateTime receivedAt,
        decimal totalAmount,
        string? supplierReference = null) => new()
    {
        ClassId = schoolClass.Id,
        Class = schoolClass,
        ReceivedAt = receivedAt,
        TotalAmount = totalAmount,
        SupplierReference = supplierReference,
        Note = "Receipt"
    };

    [Test]
    public async Task Handle_WithInclusiveDateRange_ReturnsOnlyReceiptsWithinRangeAndSummaryKpis()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass();
        var otherClass = CreateClass("Spring 2027");
        context.SchoolClasses.AddRange(schoolClass, otherClass);
        context.GoodsReceipts.AddRange(
            CreateReceipt(schoolClass, new DateTime(2026, 1, 1, 0, 0, 0), 100m, "PO-001"),
            CreateReceipt(schoolClass, new DateTime(2026, 6, 30, 23, 59, 59), 250m, "PO-002"),
            CreateReceipt(schoolClass, new DateTime(2026, 7, 1, 0, 0, 0), 999m, "PO-003"),
            CreateReceipt(otherClass, new DateTime(2026, 3, 1), 500m, "OTHER"));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassGoodsReceiptCostsHandler(context).Handle(
            new GetClassGoodsReceiptCostsQuery
            {
                ClassId = schoolClass.Id,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 6, 30)
            },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ReceiptCount.ShouldBe(2);
        result.Value.TotalAmount.ShouldBe(350m);
        result.Value.AverageAmount.ShouldBe(175m);
        result.Value.Points.Select(point => point.SupplierReference)
            .ShouldBe(["PO-001", "PO-002"]);
    }

    [Test]
    public async Task Handle_UsesStoredReceiptTotalAmountInsteadOfRecomputingFromStock()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass();
        var category = new Category { Name = "Pantry" };
        var item = new Item { Name = "Rice", Category = category };
        var location = new Location { Name = "Kitchen", Type = "StorageRoom" };
        var receipt = CreateReceipt(schoolClass, new DateTime(2026, 2, 1), 125m);
        receipt.Batches.Add(new StockBatch
        {
            Item = item,
            Location = location,
            ReceivedClass = schoolClass,
            Quantity = 2,
            UnitPrice = 3m,
            ReceivedDate = new DateOnly(2026, 2, 1),
            GoodsReceipt = receipt
        });
        context.AddRange(schoolClass, category, item, location, receipt);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassGoodsReceiptCostsHandler(context).Handle(
            new GetClassGoodsReceiptCostsQuery { ClassId = schoolClass.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Points.Single().TotalAmount.ShouldBe(125m);
        result.Value.TotalAmount.ShouldBe(125m);
    }

    [Test]
    public async Task Handle_WithUnknownClass_ReturnsSchoolClassNotFoundFailure()
    {
        await using var context = CreateContext();

        var result = await new GetClassGoodsReceiptCostsHandler(context).Handle(
            new GetClassGoodsReceiptCostsQuery { ClassId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldContain("School class");
    }
}
