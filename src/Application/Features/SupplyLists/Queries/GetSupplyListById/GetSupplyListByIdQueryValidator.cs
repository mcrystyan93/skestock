using skestock.Application.Common.Errors;

namespace skestock.Application.Features.SupplyLists.Queries.GetSupplyListById;

public class GetSupplyListByIdQueryValidator : AbstractValidator<GetSupplyListByIdQuery>
{
    public GetSupplyListByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
