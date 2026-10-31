using skestock.Application.UnitTests.Features.Items.Images;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.EditItem;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;
using skestock.Domain.Entities.Categories;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;

namespace skestock.Application.UnitTests.Features.Items.Commands.EditItem;

public class EditItemCommandValidatorTests
{
    private static async Task<(ItemTestDbContext Context, Category Category, Item Item)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ItemTestDbContext(options);
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var item = new Item { Name = "Pencil", Unit = "unit", CategoryId = category.Id };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, category, item);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenCommandIsValid()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "New Name", Unit = "unit", CategoryId = category.Id });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmpty()
    {
        var (context, category, _) = await CreateContextAsync();
        await using var _disposable = context;
        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new EditItemCommand { Id = Guid.Empty, Name = "Pencil", Unit = "unit", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(EditItemCommand.Id) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateSkuErrorWhenSkuBelongsToSameItem()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        item.Sku = "SKU-1";
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());
        var result = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Sku = "SKU-1", Unit = "unit", CategoryId = category.Id });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateSku);
    }

    [Test]
    public async Task ShouldHaveDuplicateSkuErrorWhenSkuBelongsToAnotherItem()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var otherItem = new Item { Name = "Eraser", Unit = "unit", Sku = "SKU-2", CategoryId = category.Id };
        context.Items.Add(otherItem);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());
        var result = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Sku = "SKU-2", Unit = "unit", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateSku);
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenCategoryDoesNotExist()
    {
        var (context, _, item) = await CreateContextAsync();
        await using var _disposable = context;
        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Unit = "unit", CategoryId = Guid.NewGuid() });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    private static async Task<Guid> AddLocationAsync(ItemTestDbContext context)
    {
        var location = new Location { Name = Guid.NewGuid().ToString(), Type = "Kitchen" };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);
        return location.Id;
    }

    private static EditItemCommand WithThresholds(Guid categoryId, Guid itemId, params ItemLocationThresholdDto[] thresholds) => new()
    {
        Id = itemId, Name = "Pencil", Unit = "unit", CategoryId = categoryId, LocationThresholds = [.. thresholds]
    };

    [Test]
    public async Task ShouldHaveErrorWhenLocationThresholdIsNegative()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var locationId = await AddLocationAsync(context);
        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(WithThresholds(category.Id, item.Id, new ItemLocationThresholdDto { LocationId = locationId, MinThreshold = -1 }));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(EditItemCommand.LocationThresholds) && e.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenLocationThresholdsAreEmptyOrZero()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var locationId = await AddLocationAsync(context);
        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        (await validator.ValidateAsync(WithThresholds(category.Id, item.Id))).IsValid.ShouldBeTrue();
        (await validator.ValidateAsync(WithThresholds(category.Id, item.Id, new ItemLocationThresholdDto { LocationId = locationId, MinThreshold = 0 }))).IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveDuplicateLocationErrorWhenLocationRepeats()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var locationId = await AddLocationAsync(context);
        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(WithThresholds(category.Id, item.Id,
            new ItemLocationThresholdDto { LocationId = locationId, MinThreshold = 1 },
            new ItemLocationThresholdDto { LocationId = locationId, MinThreshold = 2 }));

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateLocation);
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenLocationDoesNotExist()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(WithThresholds(category.Id, item.Id, new ItemLocationThresholdDto { LocationId = Guid.NewGuid(), MinThreshold = 1 }));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(EditItemCommand.LocationThresholds) && e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenShelfLifeDaysIsNullOrPositive()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var nullResult = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Unit = "unit", ShelfLifeDays = null, CategoryId = category.Id });
        var positiveResult = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Unit = "unit", ShelfLifeDays = 14, CategoryId = category.Id });

        nullResult.Errors.ShouldNotContain(e => e.PropertyName == nameof(EditItemCommand.ShelfLifeDays));
        positiveResult.Errors.ShouldNotContain(e => e.PropertyName == nameof(EditItemCommand.ShelfLifeDays));
    }

    [Test]
    public async Task ShouldHaveErrorWhenShelfLifeDaysIsNotPositive()
    {
        var (context, category, item) = await CreateContextAsync();
        await using var _ = context;
        var validator = new EditItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var zeroResult = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Unit = "unit", ShelfLifeDays = 0, CategoryId = category.Id });
        var negativeResult = await validator.ValidateAsync(new EditItemCommand { Id = item.Id, Name = "Pencil", Unit = "unit", ShelfLifeDays = -3, CategoryId = category.Id });

        zeroResult.Errors.ShouldContain(e => e.PropertyName == nameof(EditItemCommand.ShelfLifeDays) && e.ErrorCode == ValidationErrorCodes.GreaterThan);
        negativeResult.Errors.ShouldContain(e => e.PropertyName == nameof(EditItemCommand.ShelfLifeDays) && e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }
}
