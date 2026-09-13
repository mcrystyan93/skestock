using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Items.Queries.GetItemImportBatchById;

public class GetItemImportBatchByIdQueryValidator : AbstractValidator<GetItemImportBatchByIdQuery>
{
    public GetItemImportBatchByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
