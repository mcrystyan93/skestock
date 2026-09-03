using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

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
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();

        builder.Ignore<FileMetadata>();

        builder.Entity<UserProfile>(b =>
        {
            b.Ignore(u => u.CreatedBy);
            b.Ignore(u => u.LastModifiedBy);
        });

        builder.Entity<Category>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
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
        var handler = new CreateItemCommandHandler(context);

        var result = await handler.Handle(new CreateItemCommand
        {
            Name = "Pencil",
            Unit = "box",
            MinThreshold = 5,
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Pencil");
        result.Value.Unit.ShouldBe("box");
        result.Value.MinThreshold.ShouldBe(5);
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
        var handler = new CreateItemCommandHandler(context);

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
        var handler = new CreateItemCommandHandler(context);

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
        var handler = new CreateItemCommandHandler(context);

        var first = await handler.Handle(new CreateItemCommand { Name = "Pen", Unit = "unit", CategoryId = category.Id }, CancellationToken.None);
        var second = await handler.Handle(new CreateItemCommand { Name = "Marker", Unit = "unit", CategoryId = category.Id }, CancellationToken.None);

        first.Value.Id.ShouldNotBe(second.Value.Id);
        (await context.Items.CountAsync(CancellationToken.None)).ShouldBe(2);
    }
}
