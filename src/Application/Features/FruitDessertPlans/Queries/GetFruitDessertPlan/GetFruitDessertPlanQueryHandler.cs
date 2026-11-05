using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Application.Features.FruitDessertPlans.Models;

namespace skestock.Application.Features.FruitDessertPlans.Queries.GetFruitDessertPlan;

public class GetFruitDessertPlanQueryHandler(
    IApplicationDbContext dbContext,
    IWorkingDayProvider workingDayProvider,
    IPhoneNumberNormalizer phoneNumberNormalizer)
    : IRequestHandler<GetFruitDessertPlanQuery, Result<IReadOnlyList<FruitDessertPlanEntryDto>>>
{
    public async ValueTask<Result<IReadOnlyList<FruitDessertPlanEntryDto>>> Handle(
        GetFruitDessertPlanQuery request, CancellationToken cancellationToken)
    {
        var schoolClass = await dbContext.SchoolClasses
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == request.ClassId, cancellationToken);
        if (schoolClass is null)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.ClassId));

        await FruitDessertPlanSync.EnsureEntriesAsync(dbContext, workingDayProvider, schoolClass, cancellationToken);

        var rows = await FruitDessertPlanProjection.Rows(dbContext)
            .Where(r => r.SchoolClassId == request.ClassId
                        && r.Date >= schoolClass.StartDate && r.Date <= schoolClass.EndDate)
            .OrderBy(r => r.Date)
            .ToListAsync(cancellationToken);

        IReadOnlyList<FruitDessertPlanEntryDto> dtos = rows
            .Select(r => FruitDessertPlanProjection.ToDto(r, phoneNumberNormalizer))
            .ToList();
        return Result.Ok(dtos);
    }
}
