using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Statistics.Queries.GetClassDailyConsumption;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetClassDailyConsumption;

public class GetClassDailyConsumptionHandlerTests
{
    // 2026-09-10 12:00 local in Bucharest.
    private static readonly FakeTimeProvider Clock =
        new(new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero));

    [Test]
    public async Task Handle_StartsAtLocalDateOfFirstTransactionAndZeroFills()
    {
        await using var context = CreateContext();
        var (schoolClass, item, location, user) = await SeedAsync(context, new DateOnly(2026, 12, 20));

        // 2026-08-30 22:30Z is 2026-08-31 01:30 local - before the class start date.
        context.StockTransactions.Add(Transaction(
            schoolClass, item, location, user, new DateTimeOffset(2026, 8, 30, 22, 30, 0, TimeSpan.Zero)));
        context.DailyItemConsumptions.AddRange(
            Row(schoolClass, item, location, new DateOnly(2026, 9, 2), 6, 3m),
            Row(schoolClass, item, location, new DateOnly(2026, 9, 10), 5, 8m));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await Handle(context, schoolClass.Id);

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value;
        dto.FromDate.ShouldBe(new DateOnly(2026, 8, 31));
        dto.ToDate.ShouldBe(new DateOnly(2026, 9, 10));
        dto.Points.Count.ShouldBe(11);
        dto.Points[0].Quantity.ShouldBe(0);
        dto.Points.Single(p => p.Date == new DateOnly(2026, 9, 2)).Quantity.ShouldBe(6);
        dto.TotalQuantity.ShouldBe(11);
        dto.TotalValue.ShouldBe(11m);
        dto.AverageQuantity.ShouldBe(1m);
        dto.AverageValue.ShouldBe(1m);
    }

    [Test]
    public async Task Handle_EndedClass_StopsAtEndDate()
    {
        await using var context = CreateContext();
        var (schoolClass, item, location, user) = await SeedAsync(context, new DateOnly(2026, 9, 5));
        context.StockTransactions.Add(Transaction(
            schoolClass, item, location, user, new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero)));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await Handle(context, schoolClass.Id);

        result.Value.ToDate.ShouldBe(new DateOnly(2026, 9, 5));
        result.Value.Points.Count.ShouldBe(5);
    }

    [Test]
    public async Task Handle_WithoutTransactions_ReturnsEmptySeries()
    {
        await using var context = CreateContext();
        var (schoolClass, _, _, _) = await SeedAsync(context, new DateOnly(2026, 12, 20));

        var result = await Handle(context, schoolClass.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Points.ShouldBeEmpty();
        result.Value.FromDate.ShouldBeNull();
    }

    [Test]
    public async Task Handle_UnknownClass_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await Handle(context, Guid.NewGuid());

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error is SchoolClassErrors.SchoolClassNotFound);
    }

    private static ValueTask<FluentResults.Result<Application.Features.Statistics.Models.ClassDailyConsumptionDto>>
        Handle(GoodsReceiptTestDbContext context, Guid classId) =>
        new GetClassDailyConsumptionHandler(context, Clock)
            .Handle(new GetClassDailyConsumptionQuery { ClassId = classId }, CancellationToken.None);

    private static GoodsReceiptTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(SchoolClass, Item, Location, UserProfile)> SeedAsync(
        GoodsReceiptTestDbContext context,
        DateOnly endDate)
    {
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = endDate
        };
        var item = new Item { Name = "Pencils", Category = new Category { Name = "Stationery" } };
        var location = new Location { Name = "Kitchen", Type = "Kitchen" };
        var user = new UserProfile { IdentityId = Guid.NewGuid() };

        context.AddRange(schoolClass, item, location, user);
        await context.SaveChangesAsync(CancellationToken.None);
        return (schoolClass, item, location, user);
    }

    private static StockTransaction Transaction(
        SchoolClass schoolClass,
        Item item,
        Location location,
        UserProfile user,
        DateTimeOffset createdAt) => new()
    {
        Class = schoolClass,
        Item = item,
        Location = location,
        User = user,
        CreatedAt = createdAt,
        QuantityChange = 10
    };

    private static DailyItemConsumption Row(
        SchoolClass schoolClass,
        Item item,
        Location location,
        DateOnly date,
        int quantity,
        decimal value) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        ItemId = item.Id,
        ClassId = schoolClass.Id,
        LocationId = location.Id,
        Quantity = quantity,
        TotalValue = value
    };
}
