using FluentValidation.Results;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Congregations;

namespace skestock.Application.Features.Congregations;

internal static class CongregationCommandSupport
{
    // Saves a create/update. The validator's duplicate-name check is racy, so a unique-index
    // violation from a concurrent request is surfaced as the same validation error.
    public static async Task SaveAsync(
        IApplicationDbContext dbContext, Congregation congregation, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }
        catch (DbUpdateException)
        {
            if (!await CongregationNameValidation.IsNameTakenAsync(
                    dbContext, congregation.Id, congregation.Name, cancellationToken))
                throw;
        }

        throw new Common.Exceptions.ValidationException([
            new ValidationFailure(nameof(Congregation.Name), "A congregation with this name already exists")
            {
                ErrorCode = ValidationErrorCodes.DuplicateName
            }
        ]);
    }
}
