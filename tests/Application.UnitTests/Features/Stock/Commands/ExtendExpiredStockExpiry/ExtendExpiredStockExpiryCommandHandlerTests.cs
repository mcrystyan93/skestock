using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Stock.Commands.ExtendExpiredStockExpiry;
using skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Events.Stock;
using NUnit.Framework;
using Shouldly;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Users;

namespace skestock.Application.UnitTests.Features.Stock.Commands.ExtendExpiredStockExpiry;

public class ExtendExpiredStockExpiryCommandHandlerTests
{
    private static readonly DateOnly Today = new(2026, 10, 1);

    [Test]
    public async Task Handle_ExtendsOnlyExpiredBatchesFromTodayAndAudits()
    {
        var (context, item, location, schoolClass, user) = await CreateContextAsync(isPerishable: true);
        await using var _ = context;

        var expired = CreateBatch(item, location, schoolClass, 5, Today.AddDays(-1));
        var expiresToday = CreateBatch(item, location, schoolClass, 3, Today);
        var valid = CreateBatch(item, location, schoolClass, 8, Today.AddDays(10));
        var emptyExpired = CreateBatch(item, location, schoolClass, 0, Today.AddDays(-5));
        context.StockBatches.AddRange(expired, expiresToday, valid, emptyExpired);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(context, user).Handle(CreateCommand(item, location, schoolClass, 14), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await context.StockBatches.SingleAsync(b => b.Id == expired.Id)).ExpiryDate.ShouldBe(Today.AddDays(14));
        (await context.StockBatches.SingleAsync(b => b.Id == expiresToday.Id)).ExpiryDate.ShouldBe(Today.AddDays(14));
        (await context.StockBatches.SingleAsync(b => b.Id == valid.Id)).ExpiryDate.ShouldBe(Today.AddDays(10));
        (await context.StockBatches.SingleAsync(b => b.Id == emptyExpired.Id)).ExpiryDate.ShouldBe(Today.AddDays(-5));
        (await context.StockBatches.SingleAsync(b => b.Id == expired.Id)).Quantity.ShouldBe(5);
        (await context.Items.SingleAsync()).ShelfLifeDays.ShouldBe(item.ShelfLifeDays);

        var transactions = await context.StockTransactions.ToListAsync();
        transactions.Count.ShouldBe(2);
        transactions.ShouldAllBe(t => t.QuantityChange == 0
                                      && t.Type == StockTransactionType.Adjustment
                                      && t.UserId == user.IdentityId
                                      && t.Reason!.StartsWith(ExtendExpiredStockExpiryCommandHandler.ReasonPrefix));
        expired.DomainEvents.OfType<StockAdjustedEvent>().Count().ShouldBe(1);
    }

    [Test]
    public async Task Handle_WhenNoExpiredBatches_ReturnsNoExpiredQuantity()
    {
        var (context, item, location, schoolClass, user) = await CreateContextAsync(isPerishable: true);
        await using var _ = context;
        context.StockBatches.Add(CreateBatch(item, location, schoolClass, 8, Today.AddDays(1)));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(context, user).Handle(CreateCommand(item, location, schoolClass, 7), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Metadata[ErrorMetadataKeys.Code].ShouldBe(StockErrors.NoExpiredQuantity.ErrorCode);
        (await context.StockTransactions.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenItemIsNotPerishable_ReturnsNoExpiredQuantity()
    {
        var (context, item, location, schoolClass, user) = await CreateContextAsync(isPerishable: false);
        await using var _ = context;
        var batch = CreateBatch(item, location, schoolClass, 5, Today.AddDays(-3));
        context.StockBatches.Add(batch);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await CreateHandler(context, user).Handle(CreateCommand(item, location, schoolClass, 7), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        (await context.StockBatches.SingleAsync()).ExpiryDate.ShouldBe(Today.AddDays(-3));
    }

    private static ExtendExpiredStockExpiryCommandHandler CreateHandler(DbContext context, UserProfile user)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
        return new ExtendExpiredStockExpiryCommandHandler(
            (skestock.Application.Common.Interfaces.IApplicationDbContext)context,
            new FakeUser(user.IdentityId),
            time);
    }

    private static ExtendExpiredStockExpiryCommand CreateCommand(Item item, Location location, SchoolClass schoolClass, int days) => new()
    {
        ClassId = schoolClass.Id,
        ItemId = item.Id,
        LocationId = location.Id,
        ExtensionDays = days
    };

    private static async Task<(GoodsReceiptTestDbContext Context, Item Item, Location Location, SchoolClass Class, UserProfile User)> CreateContextAsync(bool isPerishable)
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new GoodsReceiptTestDbContext(options);
        var category = new Category { Name = "Pantry" };
        var item = new Item { Name = "Milk", Unit = "buc", MinThreshold = 2, IsPerishable = isPerishable, ShelfLifeDays = isPerishable ? 7 : null, Category = category };
        var location = new Location { Name = "Main Storage", Type = "StorageRoom" };
        var schoolClass = new SchoolClass { Name = "Fall 2026", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 20) };
        var user = new UserProfile { IdentityId = Guid.NewGuid(), FirstName = "Staff", LastName = "Member" };
        context.Categories.Add(category);
        context.Items.Add(item);
        context.Locations.Add(location);
        context.SchoolClasses.Add(schoolClass);
        context.UserProfiles.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, item, location, schoolClass, user);
    }

    private static StockBatch CreateBatch(Item item, Location location, SchoolClass schoolClass, int quantity, DateOnly expiry) => new()
    {
        ItemId = item.Id,
        LocationId = location.Id,
        ReceivedClassId = schoolClass.Id,
        Quantity = quantity,
        ExpiryDate = expiry,
        ReceivedDate = expiry.AddDays(-10),
        UnitPrice = 2.5m
    };
}
