using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.UnitTests.Features.Items.Images;
using skestock.Application.Features.Items;
using skestock.Application.Features.Items.Commands.GenerateItemThumbnail;
using System.Text.Json;
using skestock.Shared;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Application.Features.Items.Models;
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

namespace skestock.Application.UnitTests.Features.Items.Commands.CreateItem;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler/validator tests. Only
/// maps <see cref="Item"/>, the <see cref="Category"/> it requires, and the
/// <see cref="UserProfile"/> referenced for CreatedBy/LastModifiedBy; all other DbSets required
/// by the interface are left unmapped/ignored since <see cref="CreateItemCommandHandler"/> never
/// touches them. Mirrors CategoryTestDbContext/LocationTestDbContext and avoids EF's "ambiguous
/// one-to-one relationship" error the full production model would trigger for UserProfile.
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
    public DbSet<ItemLocationThreshold> ItemLocationThresholds => Set<ItemLocationThreshold>();
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

        builder.Entity<Location>(b =>
        {
            b.HasOne(l => l.CreatedBy).WithMany().HasForeignKey(l => l.CreatedById);
            b.HasOne(l => l.LastModifiedBy).WithMany().HasForeignKey(l => l.LastModifiedById);
            b.HasOne(l => l.ParentLocation).WithMany(l => l.ChildLocations).HasForeignKey(l => l.ParentLocationId);
            b.Ignore(l => l.Batches);
            b.Ignore(l => l.Transactions);
            b.Ignore(l => l.ClassBalances);
        });
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class CreateItemCommandHandlerTests
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
    public async Task Handle_WithValidData_PersistsItemAndReturnsDto()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemCommandHandler(context, ItemImageTestSupport.User());

        var result = await handler.Handle(new CreateItemCommand
        {
            Name = "Pencil",
            Unit = "box",
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Pencil");
        result.Value.Unit.ShouldBe("box");
        result.Value.IsActive.ShouldBeTrue();
        result.Value.CategoryId.ShouldBe(category.Id);
        result.Value.CategoryName.ShouldBe("Stationery");
        result.Value.Id.ShouldNotBe(Guid.Empty);

        var persisted = await context.Items.SingleAsync(CancellationToken.None);
        persisted.Name.ShouldBe("Pencil");
        persisted.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithSkuAndDescription_PersistsTrimmedValues()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemCommandHandler(context, ItemImageTestSupport.User());

        var result = await handler.Handle(new CreateItemCommand
        {
            Sku = "  SKU-001  ",
            Name = "  Notebook  ",
            Description = "  A5 notebook  ",
            Unit = "  unit  ",
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Sku.ShouldBe("SKU-001");
        result.Value.Name.ShouldBe("Notebook");
        result.Value.Description.ShouldBe("A5 notebook");
        result.Value.Unit.ShouldBe("unit");
    }

    [Test]
    public async Task Handle_WithoutSkuOrDescription_PersistsNullValues()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemCommandHandler(context, ItemImageTestSupport.User());

        var result = await handler.Handle(new CreateItemCommand
        {
            Name = "Eraser",
            Unit = "unit",
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Sku.ShouldBeNull();
        result.Value.Description.ShouldBeNull();
    }

    [Test]
    public async Task Handle_CalledTwice_PersistsTwoIndependentItems()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemCommandHandler(context, ItemImageTestSupport.User());

        var first = await handler.Handle(new CreateItemCommand { Name = "Pen", Unit = "unit", CategoryId = category.Id }, CancellationToken.None);
        var second = await handler.Handle(new CreateItemCommand { Name = "Marker", Unit = "unit", CategoryId = category.Id }, CancellationToken.None);

        first.Value.Id.ShouldNotBe(second.Value.Id);
        (await context.Items.CountAsync(CancellationToken.None)).ShouldBe(2);
    }

    [Test]
    public async Task Handle_WithShelfLifeDays_PersistsAndReturnsValue()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemCommandHandler(context, ItemImageTestSupport.User());

        var result = await handler.Handle(new CreateItemCommand
        {
            Name = "Milk",
            Unit = "l",
            IsPerishable = true,
            ShelfLifeDays = 7,
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsPerishable.ShouldBeTrue();
        result.Value.ShelfLifeDays.ShouldBe(7);

        var persisted = await context.Items.SingleAsync(CancellationToken.None);
        persisted.ShelfLifeDays.ShouldBe(7);
    }

    [Test]
    public async Task Handle_WithoutShelfLifeDays_PersistsNull()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemCommandHandler(context, ItemImageTestSupport.User());

        var result = await handler.Handle(new CreateItemCommand
        {
            Name = "Pen",
            Unit = "unit",
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShelfLifeDays.ShouldBeNull();

        var persisted = await context.Items.SingleAsync(CancellationToken.None);
        persisted.ShelfLifeDays.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithImage_SetsImageFileIdAndEnqueuesThumbnailJob()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var userId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var handler = new CreateItemCommandHandler(context, ItemImageTestSupport.User(userId));

        var result = await handler.Handle(new CreateItemCommand
        {
            Name = "Pencil", Unit = "unit", CategoryId = category.Id, ImageFileId = fileId
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ImageFileId.ShouldBe(fileId);
        (await context.Items.SingleAsync(CancellationToken.None)).ImageFileId.ShouldBe(fileId);

        var outbox = await context.OutboxMessages.SingleAsync(CancellationToken.None);
        outbox.QueueName.ShouldBe(Services.ItemImageThumbnailQueue);
        outbox.Type.ShouldBe(typeof(GenerateItemThumbnailCommand).AssemblyQualifiedName);
        outbox.UserId.ShouldBe(userId);
        var payload = JsonSerializer.Deserialize<GenerateItemThumbnailCommand>(outbox.Payload)!;
        payload.ItemId.ShouldBe(result.Value.Id);
        payload.ImageFileId.ShouldBe(fileId);
    }

    [Test]
    public async Task Handle_WithoutImage_EnqueuesNothing()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemCommandHandler(context, ItemImageTestSupport.User());

        var result = await handler.Handle(new CreateItemCommand
        {
            Name = "Pencil", Unit = "unit", CategoryId = category.Id
        }, CancellationToken.None);

        result.Value.ImageFileId.ShouldBeNull();
        context.OutboxMessages.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_WithLocationThresholds_PersistsRowsAndReturnsThemInDto()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var locationA = Guid.NewGuid();
        var locationB = Guid.NewGuid();
        var handler = new CreateItemCommandHandler(context, ItemImageTestSupport.User());

        var result = await handler.Handle(new CreateItemCommand
        {
            Name = "Pencil",
            Unit = "box",
            CategoryId = category.Id,
            LocationThresholds =
            [
                new ItemLocationThresholdDto { LocationId = locationA, MinThreshold = 10 },
                new ItemLocationThresholdDto { LocationId = locationB, MinThreshold = 1 }
            ]
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.LocationThresholds.Count.ShouldBe(2);
        var rows = await context.ItemLocationThresholds.ToListAsync(CancellationToken.None);
        rows.ShouldAllBe(r => r.ItemId == result.Value.Id);
        rows.Single(r => r.LocationId == locationA).MinThreshold.ShouldBe(10);
        rows.Single(r => r.LocationId == locationB).MinThreshold.ShouldBe(1);
    }

    [Test]
    public async Task Handle_WithoutLocationThresholds_PersistsNoRows()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var handler = new CreateItemCommandHandler(context, ItemImageTestSupport.User());

        var result = await handler.Handle(new CreateItemCommand { Name = "Pencil", Unit = "box", CategoryId = category.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await context.ItemLocationThresholds.CountAsync(CancellationToken.None)).ShouldBe(0);
    }
}
