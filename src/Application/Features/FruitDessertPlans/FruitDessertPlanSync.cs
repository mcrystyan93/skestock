using skestock.Application.Common.Interfaces;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertPlans;

internal static class FruitDessertPlanSync
{
    // Idempotent: inserts only the working days without a row. A closed class is left untouched.
    public static async Task EnsureEntriesAsync(
        IApplicationDbContext dbContext,
        IWorkingDayProvider workingDayProvider,
        SchoolClass schoolClass,
        CancellationToken cancellationToken)
    {
        if (schoolClass.Status == ClassStatus.Closed)
            return;

        var existing = await dbContext.FruitDessertPlanEntries
            .Where(e => e.SchoolClassId == schoolClass.Id)
            .Select(e => e.Date)
            .ToListAsync(cancellationToken);

        var missing = workingDayProvider.GetWorkingDays(schoolClass.StartDate, schoolClass.EndDate)
            .Except(existing)
            .ToList();
        if (missing.Count == 0)
            return;

        dbContext.FruitDessertPlanEntries.AddRange(missing.Select(date =>
            new FruitDessertPlanEntry { SchoolClassId = schoolClass.Id, Date = date }));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent first listing won the (SchoolClassId, Date) unique index and inserted the same days;
            // drop our failed inserts and let the caller read what is there.
            dbContext.ChangeTracker.Clear();
        }
    }
}
