using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Application.Features.FruitDessertPlans.Models;

namespace skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertEmailSent;

public class SetFruitDessertEmailSentCommandHandler(
    IApplicationDbContext dbContext,
    IPhoneNumberNormalizer phoneNumberNormalizer)
    : IRequestHandler<SetFruitDessertEmailSentCommand, Result<FruitDessertPlanEntryDto>>
{
    public async ValueTask<Result<FruitDessertPlanEntryDto>> Handle(
        SetFruitDessertEmailSentCommand request, CancellationToken cancellationToken)
    {
        var entry = await dbContext.FruitDessertPlanEntries
            .SingleOrDefaultAsync(e => e.Id == request.EntryId && e.SchoolClassId == request.ClassId, cancellationToken);
        if (entry is null)
            return Result.Fail(new FruitDessertErrors.EntryNotFound(request.EntryId));

        entry.EmailSent = request.EmailSent;
        await dbContext.SaveChangesAsync(cancellationToken);

        var row = await FruitDessertPlanProjection.Rows(dbContext)
            .SingleAsync(r => r.Id == entry.Id, cancellationToken);
        return Result.Ok(FruitDessertPlanProjection.ToDto(row, phoneNumberNormalizer));
    }
}
