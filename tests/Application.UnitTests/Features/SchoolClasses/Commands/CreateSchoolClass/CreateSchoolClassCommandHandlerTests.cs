using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;
using skestock.Application.Features.SchoolClasses.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Commands.CreateSchoolClass;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler/validator tests. Only
/// maps <see cref="SchoolClass"/> and the <see cref="UserProfile"/> it references for
/// CreatedBy/LastModifiedBy; all other DbSets required by the interface are left
/// unmapped/ignored since the SchoolClasses handlers/validators never touch them. Mirrors
/// CategoryTestDbContext/LocationTestDbContext and avoids EF's "ambiguous one-to-one
/// relationship" error the full production model would trigger for UserProfile.
/// </summary>
public class SchoolClassTestDbContext(DbContextOptions<SchoolClassTestDbContext> options)
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

        builder.Entity<SchoolClass>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.Ignore(c => c.BatchesReceived);
            b.Ignore(c => c.Transactions);
            b.Ignore(c => c.Balances);
        });

        builder.Ignore<Category>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<Location>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class CreateSchoolClassCommandHandlerTests
{
    private static SchoolClassTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SchoolClassTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SchoolClassTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithValidData_PersistsSchoolClassAndReturnsDto()
    {
        await using var context = CreateContext();
        var handler = new CreateSchoolClassCommandHandler(context);

        var result = await handler.Handle(new CreateSchoolClassCommand
        {
            Name = "Fall 2026 - Cycle 1",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 15),
            Status = ClassStatus.Upcoming
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Fall 2026 - Cycle 1");
        result.Value.StartDate.ShouldBe(new DateOnly(2026, 9, 1));
        result.Value.EndDate.ShouldBe(new DateOnly(2026, 12, 15));
        result.Value.Status.ShouldBe(ClassStatus.Upcoming);
        result.Value.Id.ShouldBeGreaterThan(0);

        var persisted = await context.SchoolClasses.SingleAsync(c => c.Id == result.Value.Id, CancellationToken.None);
        persisted.Name.ShouldBe("Fall 2026 - Cycle 1");
    }

    [Test]
    public async Task Handle_TrimsName()
    {
        await using var context = CreateContext();
        var handler = new CreateSchoolClassCommandHandler(context);

        var result = await handler.Handle(new CreateSchoolClassCommand
        {
            Name = "  Spring 2027  ",
            StartDate = new DateOnly(2027, 1, 1),
            EndDate = new DateOnly(2027, 6, 1)
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Spring 2027");
    }
}
