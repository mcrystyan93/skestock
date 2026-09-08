using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Categories.Queries.GetCategoryImportById;

public class GetCategoryImportByIdQueryValidator : AbstractValidator<GetCategoryImportByIdQuery>
{
    public GetCategoryImportByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
