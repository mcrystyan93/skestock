using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Application.Features.FruitDessertPlans.Models;

namespace skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertCongregation;

public class SetFruitDessertCongregationCommandHandler(
    IApplicationDbContext dbContext,
    IPhoneNumberNormalizer phoneNumberNormalizer)
    : IRequestHandler<SetFruitDessertCongregationCommand, Result<FruitDessertPlanEntryDto>>
{
    public async ValueTask<Result<FruitDessertPlanEntryDto>> Handle(
        SetFruitDessertCongregationCommand request, CancellationToken cancellationToken)
    {
        var entry = await dbContext.FruitDessertPlanEntries
            .SingleOrDefaultAsync(e => e.Id == request.EntryId && e.SchoolClassId == request.ClassId, cancellationToken);
        if (entry is null)
            return Result.Fail(new FruitDessertErrors.EntryNotFound(request.EntryId));

        if (request.CongregationId is { } congregationId)
        {
            // The filtered set: a soft-deleted congregation cannot be newly assigned.
            var exists = await dbContext.Congregations.AnyAsync(c => c.Id == congregationId, cancellationToken);
            if (!exists)
                return Result.Fail(new CongregationErrors.CongregationNotFound(congregationId));
        }

        entry.CongregationId = request.CongregationId;
        await dbContext.SaveChangesAsync(cancellationToken);

        var row = await FruitDessertPlanProjection.Rows(dbContext)
            .SingleAsync(r => r.Id == entry.Id, cancellationToken);
        return Result.Ok(FruitDessertPlanProjection.ToDto(row, phoneNumberNormalizer));
    }
}
