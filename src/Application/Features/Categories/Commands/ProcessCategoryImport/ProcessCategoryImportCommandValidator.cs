using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Categories.Commands.ProcessCategoryImport;

public class ProcessCategoryImportCommandValidator : AbstractValidator<ProcessCategoryImportCommand>
{
    public ProcessCategoryImportCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.CategoryImportId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                RuleFor(x => x.CategoryImportId)
                    .MustAsync((categoryImportId, cancellationToken) =>
                        CategoryImportExistsAsync(dbContext, categoryImportId, cancellationToken))
                    .WithMessage("Category import does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }

    private static async Task<bool> CategoryImportExistsAsync(IApplicationDbContext dbContext,
        Guid categoryImportId, CancellationToken cancellationToken)
    {
        return await dbContext.CategoryImports
            .AsNoTracking()
            .AnyAsync(x => x.Id == categoryImportId, cancellationToken);
    }
}
