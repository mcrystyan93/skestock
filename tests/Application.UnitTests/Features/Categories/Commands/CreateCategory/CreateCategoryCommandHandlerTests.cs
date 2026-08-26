using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Commands.CreateCategory;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Commands.CreateCategory;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler/validator tests. Only
/// maps <see cref="Category"/> (and the <see cref="UserProfile"/> it references for
/// CreatedBy/LastModifiedBy); all other DbSets required by the interface are left
/// unmapped/ignored since <see cref="CreateCategoryCommandHandler"/> never touches them. This
/// mirrors the equivalent test double in GetAllCategoriesHandlerTests and avoids EF's "ambiguous
/// one-to-one relationship" error the full production model would trigger for UserProfile.
/// </summary>
public class CategoryTestDbContext(DbContextOptions<CategoryTestDbContext> options)
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserProfile>(b =>
        {
            b.Ignore(u => u.CreatedBy);
            b.Ignore(u => u.LastModifiedBy);
        });

        builder.Entity<Category>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.Ignore(c => c.Items);
        });

        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class CreateCategoryCommandHandlerTests
{
    private static CategoryTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CategoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CategoryTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithValidName_PersistsCategoryAndReturnsDto()
    {
        await using var context = CreateContext();
        var handler = new CreateCategoryCommandHandler(context);

        var result = await handler.Handle(new CreateCategoryCommand { Name = "Stationery" }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Stationery");
        result.Value.Id.ShouldBeGreaterThan(0);

        var persisted = await context.Categories.SingleAsync(CancellationToken.None);
        persisted.Name.ShouldBe("Stationery");
        persisted.Id.ShouldBe(result.Value.Id);
    }

    [Test]
    public async Task Handle_WithNameContainingLeadingOrTrailingWhitespace_TrimsBeforePersisting()
    {
        await using var context = CreateContext();
        var handler = new CreateCategoryCommandHandler(context);

        var result = await handler.Handle(new CreateCategoryCommand { Name = "  Office Supplies  " }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Office Supplies");

        var persisted = await context.Categories.SingleAsync(CancellationToken.None);
        persisted.Name.ShouldBe("Office Supplies");
    }

    [Test]
    public async Task Handle_CalledTwice_PersistsTwoIndependentCategories()
    {
        await using var context = CreateContext();
        var handler = new CreateCategoryCommandHandler(context);

        var first = await handler.Handle(new CreateCategoryCommand { Name = "Books" }, CancellationToken.None);
        var second = await handler.Handle(new CreateCategoryCommand { Name = "Electronics" }, CancellationToken.None);

        first.Value.Id.ShouldNotBe(second.Value.Id);
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(2);
    }
}
