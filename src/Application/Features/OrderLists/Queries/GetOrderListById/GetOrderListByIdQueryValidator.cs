using skestock.Application.Common.Errors;

namespace skestock.Application.Features.OrderLists.Queries.GetOrderListById;

public class GetOrderListByIdQueryValidator : AbstractValidator<GetOrderListByIdQuery>
{
    public GetOrderListByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
