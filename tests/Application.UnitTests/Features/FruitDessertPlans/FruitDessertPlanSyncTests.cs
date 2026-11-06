using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.FruitDessertPlans.Queries.GetFruitDessertPlan;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;
using skestock.Infrastructure.FruitDessert;

namespace skestock.Application.UnitTests.Features.FruitDessertPlans;

public class FruitDessertPlanSyncTests
{
    private sealed class FailingSaveContext(DbContextOptions<FruitDessertTestDbContext> options)
        : FruitDessertTestDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("boom", new InvalidOperationException("not a unique violation"));
    }

    [Test]
    public async Task Handle_SaveFailsWithoutUniqueViolation_Rethrows()
    {
        var options = new DbContextOptionsBuilder<FruitDessertTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var schoolClass = new SchoolClass
        {
            Name = "Toamna", StartDate = new DateOnly(2026, 11, 2), EndDate = new DateOnly(2026, 11, 13), Status = ClassStatus.Active
        };
        await using (var seed = new FruitDessertTestDbContext(options))
        {
            seed.SchoolClasses.Add(schoolClass);
            await seed.SaveChangesAsync();
        }

        await using var context = new FailingSaveContext(options);
        var handler = new GetFruitDessertPlanQueryHandler(context, new WorkingDayProvider(), new PhoneNumberNormalizer());

        await Should.ThrowAsync<DbUpdateException>(() =>
            handler.Handle(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id }, CancellationToken.None).AsTask());
    }
}
