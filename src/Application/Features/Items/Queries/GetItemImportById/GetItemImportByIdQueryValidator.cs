using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Items.Queries.GetItemImportById;

public class GetItemImportByIdQueryValidator : AbstractValidator<GetItemImportByIdQuery>
{
    public GetItemImportByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
