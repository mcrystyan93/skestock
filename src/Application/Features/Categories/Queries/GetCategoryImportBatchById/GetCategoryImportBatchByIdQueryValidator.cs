using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Categories.Queries.GetCategoryImportBatchById;

public class GetCategoryImportBatchByIdQueryValidator : AbstractValidator<GetCategoryImportBatchByIdQuery>
{
    public GetCategoryImportBatchByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
