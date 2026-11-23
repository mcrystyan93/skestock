using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Statistics.Queries.GetClassEvolution;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities.Expenses;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetClassEvolution;

public class GetClassEvolutionHandlerTests
{
    private static GoodsReceiptTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Test]
    public async Task Handle_SpansFirstToLastWeekAndZeroFillsGaps()
    {
        await using var context = CreateContext();
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };
        context.SchoolClasses.Add(schoolClass);
        context.GoodsReceipts.AddRange(
            new GoodsReceipt { ClassId = schoolClass.Id, Class = schoolClass, TotalAmount = 100m, Note = "n", ReceivedAt = new DateTimeOffset(2025, 11, 12, 0, 0, 0, TimeSpan.Zero) },
            new GoodsReceipt { ClassId = schoolClass.Id, Class = schoolClass, TotalAmount = 40m, Note = "n", ReceivedAt = new DateTimeOffset(2025, 11, 14, 0, 0, 0, TimeSpan.Zero) });
        context.Expenses.AddRange(
            new Expense { SchoolClassId = schoolClass.Id, Status = ExpenseStatus.Processed, TotalAmount = 30m, ExpenseDate = new DateOnly(2025, 11, 26) },
            new Expense { SchoolClassId = schoolClass.Id, Status = ExpenseStatus.Processed, TotalAmount = 99m, ExpenseDate = null });
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassEvolutionQueryHandler(context).Handle(
            new GetClassEvolutionQuery { ClassId = schoolClass.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var points = result.Value.Points;
        // 12 and 14 Nov 2025 share the week of Mon 10 Nov; 26 Nov falls in the week of Mon 24 Nov.
        points.Select(p => p.WeekStart).ShouldBe(
        [
            new DateOnly(2025, 11, 10), new DateOnly(2025, 11, 17), new DateOnly(2025, 11, 24)
        ]);
        points[0].Orders.ShouldBe(140m);
        points[1].Orders.ShouldBe(0m);
        points[2].Expenses.ShouldBe(30m);
    }

    [Test]
    public async Task Handle_NoEntries_ReturnsNoPoints()
    {
        await using var context = CreateContext();
        var schoolClass = new SchoolClass
        {
            Name = "Empty",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31)
        };
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetClassEvolutionQueryHandler(context).Handle(
            new GetClassEvolutionQuery { ClassId = schoolClass.Id }, CancellationToken.None);

        result.Value.Points.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_UnknownClass_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await new GetClassEvolutionQueryHandler(context).Handle(
            new GetClassEvolutionQuery { ClassId = Guid.NewGuid() }, CancellationToken.None);

        result.Errors[0].ShouldBeOfType<SchoolClassErrors.SchoolClassNotFound>();
    }
}
