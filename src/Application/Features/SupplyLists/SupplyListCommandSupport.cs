using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Common;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SupplyLists;

namespace skestock.Application.Features.SupplyLists;

// Behaviour shared by the create/update/enable/disable handlers.
internal static class SupplyListCommandSupport
{
    // Saves a create/update. The validator's duplicate-name check is racy, so a unique-index
    // violation from a concurrent request is surfaced as the same validation error.
    public static async Task SaveAsync(
        IApplicationDbContext dbContext, SupplyList supplyList, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }
        catch (DbUpdateException)
        {
            if (!await SupplyListNameValidation.IsNameTakenAsync(
                    dbContext, supplyList.Id, supplyList.Name, cancellationToken))
                throw;
        }

        throw new Common.Exceptions.ValidationException([
            new ValidationFailure(nameof(SupplyList.Name), "A supply list with this name already exists")
            {
                ErrorCode = ValidationErrorCodes.DuplicateName
            }
        ]);
    }

    public static async Task<Result<SupplyListDto>> LoadResultAsync(
        IApplicationDbContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        var dto = await SupplyListProjection.LoadAsync(dbContext, id, cancellationToken);
        return dto is null
            ? Result.Fail(new SupplyListErrors.SupplyListNotFound(id))
            : Result.Ok(dto);
    }

    // Repeated requests succeed; only an actual state change is saved and broadcast.
    public static async Task<Result<SupplyListDto>> SetActiveAsync(
        IApplicationDbContext dbContext,
        Guid id,
        bool isActive,
        Func<SupplyList, BaseEvent> createEvent,
        CancellationToken cancellationToken)
    {
        var supplyList = await dbContext.SupplyLists.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (supplyList is null)
            return Result.Fail(new SupplyListErrors.SupplyListNotFound(id));

        if (supplyList.IsActive != isActive)
        {
            supplyList.IsActive = isActive;
            supplyList.AddDomainEvent(createEvent(supplyList));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await LoadResultAsync(dbContext, id, cancellationToken);
    }
}
