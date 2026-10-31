using skestock.Application.UnitTests.Features.Items.Images;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;
using skestock.Domain.Entities.Categories;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;

namespace skestock.Application.UnitTests.Features.Items.Commands.CreateItem;

public class CreateItemCommandValidatorTests
{
    private static async Task<(ItemTestDbContext Context, Category Category)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ItemTestDbContext(options);
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, category);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenCommandIsValid()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", CategoryId = category.Id });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenNameIsEmpty()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "", Unit = "unit", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.Name) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenUnitIsEmpty()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.Unit) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    private static async Task<Guid> AddLocationAsync(ItemTestDbContext context)
    {
        var location = new Location { Name = Guid.NewGuid().ToString(), Type = "Kitchen" };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);
        return location.Id;
    }

    private static CreateItemCommand WithThresholds(Guid categoryId, params ItemLocationThresholdDto[] thresholds) => new()
    {
        Name = "Pencil", Unit = "unit", CategoryId = categoryId, LocationThresholds = [.. thresholds]
    };

    [Test]
    public async Task ShouldHaveErrorWhenLocationThresholdIsNegative()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var locationId = await AddLocationAsync(context);
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(WithThresholds(category.Id, new ItemLocationThresholdDto { LocationId = locationId, MinThreshold = -1 }));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.LocationThresholds) && e.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenLocationThresholdsAreEmptyOrZero()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var locationId = await AddLocationAsync(context);
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        (await validator.ValidateAsync(WithThresholds(category.Id))).IsValid.ShouldBeTrue();
        (await validator.ValidateAsync(WithThresholds(category.Id, new ItemLocationThresholdDto { LocationId = locationId, MinThreshold = 0 }))).IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldReturnRequiredErrorInsteadOfThrowingWhenThresholdEntryIsNull()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(WithThresholds(category.Id, new ItemLocationThresholdDto[] { null! }));

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveDuplicateLocationErrorWhenLocationRepeats()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var locationId = await AddLocationAsync(context);
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(WithThresholds(category.Id,
            new ItemLocationThresholdDto { LocationId = locationId, MinThreshold = 1 },
            new ItemLocationThresholdDto { LocationId = locationId, MinThreshold = 2 }));

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateLocation);
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenLocationDoesNotExist()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(WithThresholds(category.Id, new ItemLocationThresholdDto { LocationId = Guid.NewGuid(), MinThreshold = 1 }));

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.LocationThresholds) && e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenCategoryIdIsEmpty()
    {
        var (context, _) = await CreateContextAsync();
        await using var __ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", CategoryId = Guid.Empty });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.CategoryId) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenCategoryDoesNotExist()
    {
        var (context, _) = await CreateContextAsync();
        await using var __ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", CategoryId = Guid.NewGuid() });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldHaveDuplicateSkuErrorWhenSkuAlreadyExists()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        context.Items.Add(new Item { Name = "Pencil", Unit = "unit", Sku = "SKU-1", CategoryId = category.Id });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());
        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Other", Unit = "unit", Sku = "SKU-1", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateSku);
    }

    [Test]
    public async Task ShouldHaveDuplicateSkuErrorWhenSkuDiffersOnlyByCaseOrWhitespace()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        context.Items.Add(new Item { Name = "Pencil", Unit = "unit", Sku = "SKU-1", CategoryId = category.Id });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());
        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Other", Unit = "unit", Sku = "  sku-1  ", CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateSku);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateSkuErrorWhenSkuIsNull()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        context.Items.Add(new Item { Name = "Pencil", Unit = "unit", Sku = null, CategoryId = category.Id });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());
        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Other", Unit = "unit", CategoryId = category.Id });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateSku);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenShelfLifeDaysIsNull()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", ShelfLifeDays = null, CategoryId = category.Id });

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(CreateItemCommand.ShelfLifeDays));
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenShelfLifeDaysIsPositive()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", ShelfLifeDays = 30, CategoryId = category.Id });

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(CreateItemCommand.ShelfLifeDays));
    }

    [Test]
    public async Task ShouldHaveErrorWhenShelfLifeDaysIsZero()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", ShelfLifeDays = 0, CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.ShelfLifeDays) && e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }

    [Test]
    public async Task ShouldHaveErrorWhenShelfLifeDaysIsNegative()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemCommandValidator(context, ItemImageTestSupport.Blobs(), ItemImageTestSupport.Thumbnails(), ItemImageTestSupport.User());

        var result = await validator.ValidateAsync(new CreateItemCommand { Name = "Pencil", Unit = "unit", ShelfLifeDays = -5, CategoryId = category.Id });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateItemCommand.ShelfLifeDays) && e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }
}
