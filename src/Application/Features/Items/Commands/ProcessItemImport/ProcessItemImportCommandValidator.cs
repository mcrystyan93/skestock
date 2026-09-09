using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Items.Commands.ProcessItemImport;

public class ProcessItemImportCommandValidator : AbstractValidator<ProcessItemImportCommand>
{
    public ProcessItemImportCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ItemImportId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                RuleFor(x => x.ItemImportId)
                    .MustAsync((itemImportId, cancellationToken) =>
                        ItemImportExistsAsync(dbContext, itemImportId, cancellationToken))
                    .WithMessage("Item import does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }

    private static async Task<bool> ItemImportExistsAsync(IApplicationDbContext dbContext,
        Guid itemImportId, CancellationToken cancellationToken)
    {
        return await dbContext.ItemImports
            .AsNoTracking()
            .AnyAsync(x => x.Id == itemImportId, cancellationToken);
    }
}
