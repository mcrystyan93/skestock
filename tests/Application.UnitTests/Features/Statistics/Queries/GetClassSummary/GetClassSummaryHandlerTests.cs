using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Statistics.Queries.GetClassSummary;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities.Expenses;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetClassSummary;

public class GetClassSummaryHandlerTests
{
    private static GoodsReceiptTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SchoolClass CreateClass(string name) => new()
    {
        Name = name,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31)
    };

    [Test]
    public async Task Handle_SumsOnlyThisClassAndProcessedExpenses()
    {
        await using var context = CreateContext();
        var schoolClass = CreateClass("Fall 2026");
        var other = CreateClass("Spring 2027");
        context.SchoolClasses.AddRange(schoolClass, other);
        context.GoodsReceipts.AddRange(
            new GoodsReceipt { ClassId = schoolClass.Id, Class = schoolClass, TotalAmount = 100m, Note = "n", ReceivedAt = DateTimeOffset.UtcNow },
            new GoodsReceipt { ClassId = schoolClass.Id, Class = schoolClass, TotalAmount = 50.5m, Note = "n", ReceivedAt = DateTimeOffset.UtcNow },
            new GoodsReceipt { ClassId = other.Id, Class = other, TotalAmount = 999m, Note = "n", ReceivedAt = DateTimeOffset.UtcNow });
        context.Expenses.AddRange(
            new Expense { SchoolClassId = schoolClass.Id, Status = ExpenseStatus.Processed, TotalAmount = 30m },
            new Expense { SchoolClassId = schoolClass.Id, Status = ExpenseStatus.Failed, TotalAmount = 70m },
            new Expense { SchoolClassId = other.Id, Status = ExpenseStatus.Processed, TotalAmount = 5m });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassSummaryQueryHandler(context).Handle(
            new GetClassSummaryQuery { ClassId = schoolClass.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.OrderCount.ShouldBe(2);
        result.Value.TotalOrders.ShouldBe(150.5m);
        result.Value.TotalExpenses.ShouldBe(30m);
        result.Value.DonationCount.ShouldBe(0);
        result.Value.TotalDonations.ShouldBe(0m);
        result.Value.CurrentStockValue.ShouldBe(0m);
    }

    [Test]
    public async Task Handle_UnknownClass_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await new GetClassSummaryQueryHandler(context).Handle(
            new GetClassSummaryQuery { ClassId = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors[0].ShouldBeOfType<SchoolClassErrors.SchoolClassNotFound>();
    }
}
