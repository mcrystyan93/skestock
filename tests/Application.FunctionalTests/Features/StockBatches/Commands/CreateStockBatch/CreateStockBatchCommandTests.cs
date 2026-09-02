using skestock.Application.Common.Exceptions;
using skestock.Application.Features.StockBatches.Commands.CreateStockBatch;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.StockBatches.Commands.CreateStockBatch;

public class CreateStockBatchCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private async Task<(Category Category, Item Item, Item PerishableItem, Location Location, SchoolClass Class)> SeedPrerequisitesAsync()
    {
        var category = new Category { Name = $"{_prefix}-Category" };
        await TestApp.AddAsync(category);

        var item = new Item { Name = $"{_prefix}-Rice", Unit = "kg", MinThreshold = 10, IsPerishable = false, CategoryId = category.Id };
        await TestApp.AddAsync(item);

        var perishableItem = new Item { Name = $"{_prefix}-Milk", Unit = "L", MinThreshold = 5, IsPerishable = true, CategoryId = category.Id };
        await TestApp.AddAsync(perishableItem);

        var location = new Location { Name = $"{_prefix}-Storage", Type = "StorageRoom" };
        await TestApp.AddAsync(location);

        var schoolClass = new SchoolClass
        {
            Name = $"{_prefix}-Class",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 6, 1)
        };
        await TestApp.AddAsync(schoolClass);

        return (category, item, perishableItem, location, schoolClass);
    }

    /// <summary>
    /// CreateStockBatchCommand is [Authorize]-guarded and writes StockTransaction.UserId, which
    /// is a FK to UserProfile.IdentityId (not UserProfile.Id - see StockTransactionConfiguration).
    /// No other functional test yet exercises an [Authorize]-guarded stock command, so a matching
    /// UserProfile row must be seeded manually alongside the Identity user TestApp creates.
    /// </summary>
    private async Task RunAsUserWithProfileAsync()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
    }

    [Test]
    public async Task Handle_WithValidData_PersistsBatchAndOrderTransactionAndReturnsDto()
    {
        var (_, item, _, location, schoolClass) = await SeedPrerequisitesAsync();
        await RunAsUserWithProfileAsync();

        var result = await TestApp.SendAsync(new CreateStockBatchCommand
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 20,
            ReceivedDate = new DateOnly(2024, 2, 1),
            UnitPrice = 4.25m
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ItemId.ShouldBe(item.Id);
        result.Value.ItemName.ShouldBe(item.Name);
        result.Value.LocationId.ShouldBe(location.Id);
        result.Value.LocationName.ShouldBe(location.Name);
        result.Value.Quantity.ShouldBe(20);
        result.Value.UnitPrice.ShouldBe(4.25m);
        result.Value.LineTotal.ShouldBe(85m);
        result.Value.GoodsReceiptId.ShouldBeNull();
        result.Value.Id.ShouldNotBe(Guid.Empty);

        var persistedBatch = await TestApp.FindAsync<StockBatch>(result.Value.Id);
        persistedBatch.ShouldNotBeNull();
        persistedBatch.ReceivedClassId.ShouldBe(schoolClass.Id);
        persistedBatch.GoodsReceiptId.ShouldBeNull();

        var transaction = await TestApp.SingleOrDefaultAsync<StockTransaction>(t => t.BatchId == result.Value.Id);
        transaction.ShouldNotBeNull();
        transaction.Type.ShouldBe(StockTransactionType.Order);
        transaction.QuantityChange.ShouldBe(20);
        transaction.GoodsReceiptId.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithPerishableItemAndNoExpiryDate_ThrowsValidationException()
    {
        var (_, _, perishableItem, location, schoolClass) = await SeedPrerequisitesAsync();
        await RunAsUserWithProfileAsync();

        var act = async () => await TestApp.SendAsync(new CreateStockBatchCommand
        {
            ItemId = perishableItem.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = 5,
            ReceivedDate = new DateOnly(2024, 2, 1)
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateStockBatchCommand.ExpiryDate));
    }

    [Test]
    public async Task Handle_WithNonExistentLocationId_ThrowsValidationException()
    {
        var (_, item, _, _, schoolClass) = await SeedPrerequisitesAsync();
        await RunAsUserWithProfileAsync();

        var act = async () => await TestApp.SendAsync(new CreateStockBatchCommand
        {
            ItemId = item.Id,
            LocationId = Guid.NewGuid(),
            ReceivedClassId = schoolClass.Id,
            Quantity = 5,
            ReceivedDate = new DateOnly(2024, 2, 1)
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateStockBatchCommand.LocationId));
    }

    [Test]
    public async Task Handle_WithNegativeQuantity_ThrowsValidationException()
    {
        var (_, item, _, location, schoolClass) = await SeedPrerequisitesAsync();
        await RunAsUserWithProfileAsync();

        var act = async () => await TestApp.SendAsync(new CreateStockBatchCommand
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = schoolClass.Id,
            Quantity = -1,
            ReceivedDate = new DateOnly(2024, 2, 1)
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateStockBatchCommand.Quantity));
    }
}
