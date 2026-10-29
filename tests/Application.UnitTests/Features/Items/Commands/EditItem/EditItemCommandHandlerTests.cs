using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using skestock.Application.Features.Items.Commands.GenerateItemThumbnail;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Application.UnitTests.Features.Items.Images;
using skestock.Domain.Enums;
using skestock.Shared;
using skestock.Application.Features.Items.Commands.EditItem;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Entities.SupplyLists;
using skestock.Domain.Entities.Users;

namespace skestock.Application.UnitTests.Features.Items.Commands.EditItem;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler/validator tests. See
/// CreateItemCommandHandlerTests' ItemTestDbContext for rationale - this mirrors it for the
/// EditItem namespace.
/// </summary>
public class ItemTestDbContext(DbContextOptions<ItemTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImportBatch> CategoryImportBatches => Set<CategoryImportBatch>();
    public DbSet<CategoryImportBatchFile> CategoryImportBatchFiles => Set<CategoryImportBatchFile>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<ClassItemStockVisibility> ClassItemStockVisibilities => Set<ClassItemStockVisibility>();
    public DbSet<DailyItemConsumption> DailyItemConsumptions => Set<DailyItemConsumption>();
    public DbSet<ItemPurchaseStatistic> ItemPurchaseStatistics => Set<ItemPurchaseStatistic>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemImportBatch> ItemImportBatches => Set<ItemImportBatch>();
    public DbSet<ItemImportBatchFile> ItemImportBatchFiles => Set<ItemImportBatchFile>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<OrderList> OrderLists => Set<OrderList>();
    public DbSet<OrderListLine> OrderListLines => Set<OrderListLine>();
    public DbSet<SupplyList> SupplyLists => Set<SupplyList>();
    public DbSet<SupplyListLine> SupplyListLines => Set<SupplyListLine>();

    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Ignore<DailyItemConsumption>();

        builder.Ignore<OrderList>();
        builder.Ignore<OrderListLine>();


        builder.Ignore<CategoryImportBatch>();
        builder.Ignore<CategoryImportBatchFile>();

        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();

        builder.Entity<FileMetadata>(b =>
        {
            b.Ignore(f => f.CreatedBy);
            b.Ignore(f => f.LastModifiedBy);
        });

        builder.Entity<UserProfile>(b =>
        {
            b.Ignore(u => u.CreatedBy);
            b.Ignore(u => u.LastModifiedBy);
        });

        builder.Entity<Category>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.OwnsOne(c => c.Icon);
        });

        builder.Entity<Item>(b =>
        {
            b.HasOne(i => i.CreatedBy).WithMany().HasForeignKey(i => i.CreatedById);
            b.HasOne(i => i.LastModifiedBy).WithMany().HasForeignKey(i => i.LastModifiedById);
            b.HasOne(i => i.Category).WithMany(c => c.Items).HasForeignKey(i => i.CategoryId);
            b.Ignore(i => i.Batches);
            b.Ignore(i => i.Transactions);
            b.Ignore(i => i.ClassBalances);
        });

        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class EditItemCommandHandlerTests
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
    public async Task Handle_WithExistingItem_UpdatesFieldsAndReturnsDto()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var item = new Item { Name = "Old Name", Unit = "unit", CategoryId = category.Id };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new EditItemCommandHandler(context, ItemImageTestSupport.User(), ItemImageTestSupport.Blobs(), NullLogger<EditItemCommandHandler>.Instance);
        var result = await handler.Handle(new EditItemCommand
        {
            Id = item.Id,
            Name = "New Name",
            Unit = "box",
            MinThreshold = 10,
            IsPerishable = true,
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("New Name");
        result.Value.Unit.ShouldBe("box");
        result.Value.MinThreshold.ShouldBe(10);
        result.Value.IsPerishable.ShouldBeTrue();

        var persisted = await context.Items.SingleAsync(i => i.Id == item.Id, CancellationToken.None);
        persisted.Name.ShouldBe("New Name");
    }

    [Test]
    public async Task Handle_DoesNotChangeIsActive()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var item = new Item { Name = "Old Name", Unit = "unit", CategoryId = category.Id, IsActive = false };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new EditItemCommandHandler(context, ItemImageTestSupport.User(), ItemImageTestSupport.Blobs(), NullLogger<EditItemCommandHandler>.Instance);
        var result = await handler.Handle(new EditItemCommand
        {
            Id = item.Id,
            Name = "New Name",
            Unit = "unit",
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WithNewCategoryId_UpdatesRelationshipAndReturnsNewCategoryName()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var otherCategory = new Category { Name = "Electronics" };
        context.Categories.Add(otherCategory);
        await context.SaveChangesAsync(CancellationToken.None);

        var item = new Item { Name = "Widget", Unit = "unit", CategoryId = category.Id };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new EditItemCommandHandler(context, ItemImageTestSupport.User(), ItemImageTestSupport.Blobs(), NullLogger<EditItemCommandHandler>.Instance);
        var result = await handler.Handle(new EditItemCommand
        {
            Id = item.Id,
            Name = "Widget",
            Unit = "unit",
            CategoryId = otherCategory.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CategoryId.ShouldBe(otherCategory.Id);
        result.Value.CategoryName.ShouldBe("Electronics");
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var handler = new EditItemCommandHandler(context, ItemImageTestSupport.User(), ItemImageTestSupport.Blobs(), NullLogger<EditItemCommandHandler>.Instance);

        var result = await handler.Handle(new EditItemCommand
        {
            Id = Guid.NewGuid(),
            Name = "Anything",
            Unit = "unit",
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is skestock.Application.Common.Errors.ItemErrors.ItemNotFound);
    }

    private sealed class ImageHarness
    {
        public required ItemTestDbContext Context { get; init; }
        public required Category Category { get; init; }
        public required Item Item { get; init; }
        public Guid? OldFileId { get; init; }
        public Mock<IBlobStorageService> Blobs { get; } = new();
        public List<DeleteDto> Deleted { get; } = [];

        public EditItemCommandHandler Handler => new(
            Context, ItemImageTestSupport.User(), Blobs.Object, NullLogger<EditItemCommandHandler>.Instance);

        public EditItemCommand Edit(Guid? fileId) => new()
        {
            Id = Item.Id, Name = "Pencil", Unit = "unit", CategoryId = Category.Id, ImageFileId = fileId
        };
    }

    private static async Task<ImageHarness> CreateImageHarnessAsync(bool hasImage)
    {
        var (context, category) = await CreateContextAsync();
        Guid? oldFileId = hasImage ? Guid.NewGuid() : null;
        if (oldFileId is { } id)
        {
            context.FileMetadata.Add(new FileMetadata
            {
                FileId = id, OriginalName = "a.png", BlobContainer = "app-files", BlobPath = "2026/10/a.png",
                ContentType = "image/png", SizeBytes = 1, Status = FileStatus.Completed
            });
        }
        var item = new Item { Name = "Pencil", Unit = "unit", CategoryId = category.Id, ImageFileId = oldFileId };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var harness = new ImageHarness { Context = context, Category = category, Item = item, OldFileId = oldFileId };
        harness.Blobs.Setup(b => b.DeleteAsync(It.IsAny<DeleteDto>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteDto, CancellationToken>((dto, _) => harness.Deleted.Add(dto))
            .Returns(Task.CompletedTask);
        return harness;
    }

    [Test]
    public async Task Handle_SameImageFileId_DoesNothingWithImages()
    {
        var h = await CreateImageHarnessAsync(hasImage: true);
        await using var _ = h.Context;

        var result = await h.Handler.Handle(h.Edit(h.OldFileId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ImageFileId.ShouldBe(h.OldFileId);
        h.Context.OutboxMessages.ShouldBeEmpty();
        h.Deleted.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_AddingImage_SetsItAndEnqueuesThumbnailJob()
    {
        var h = await CreateImageHarnessAsync(hasImage: false);
        await using var _ = h.Context;
        var fileId = Guid.NewGuid();

        var result = await h.Handler.Handle(h.Edit(fileId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ImageFileId.ShouldBe(fileId);
        var outbox = await h.Context.OutboxMessages.SingleAsync(CancellationToken.None);
        outbox.QueueName.ShouldBe(Services.ItemImageThumbnailQueue);
        var payload = System.Text.Json.JsonSerializer.Deserialize<GenerateItemThumbnailCommand>(outbox.Payload)!;
        payload.ItemId.ShouldBe(h.Item.Id);
        payload.ImageFileId.ShouldBe(fileId);
        h.Deleted.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ReplacingImage_EnqueuesNewAndDeletesOldBlobsAfterSave()
    {
        var h = await CreateImageHarnessAsync(hasImage: true);
        await using var _ = h.Context;
        var newFileId = Guid.NewGuid();

        var result = await h.Handler.Handle(h.Edit(newFileId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ImageFileId.ShouldBe(newFileId);
        var payload = System.Text.Json.JsonSerializer.Deserialize<GenerateItemThumbnailCommand>(
            (await h.Context.OutboxMessages.SingleAsync(CancellationToken.None)).Payload)!;
        payload.ImageFileId.ShouldBe(newFileId);
        (await h.Context.FileMetadata.SingleAsync(f => f.FileId == h.OldFileId, CancellationToken.None))
            .Status.ShouldBe(FileStatus.Deleted);
        h.Deleted.ShouldContain(new DeleteDto("app-files", "2026/10/a.png"));
        h.Deleted.ShouldContain(new DeleteDto("app-files", $"thumbnails/{h.OldFileId}.webp"));
    }

    [Test]
    public async Task Handle_RemovingImage_ClearsItAndDeletesOldBlobs()
    {
        var h = await CreateImageHarnessAsync(hasImage: true);
        await using var _ = h.Context;

        var result = await h.Handler.Handle(h.Edit(null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ImageFileId.ShouldBeNull();
        (await h.Context.Items.SingleAsync(CancellationToken.None)).ImageFileId.ShouldBeNull();
        h.Context.OutboxMessages.ShouldBeEmpty();
        h.Deleted.Count.ShouldBe(2);
    }

    [Test]
    public async Task Handle_WhenBlobDeleteThrows_StillSucceeds()
    {
        var h = await CreateImageHarnessAsync(hasImage: true);
        await using var _ = h.Context;
        h.Blobs.Setup(b => b.DeleteAsync(It.IsAny<DeleteDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage down"));

        var result = await h.Handler.Handle(h.Edit(null), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await h.Context.Items.SingleAsync(CancellationToken.None)).ImageFileId.ShouldBeNull();
    }
}
