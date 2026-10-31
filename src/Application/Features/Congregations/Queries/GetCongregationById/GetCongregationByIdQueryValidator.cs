using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Congregations.Queries.GetCongregationById;

public class GetCongregationByIdQueryValidator : AbstractValidator<GetCongregationByIdQuery>
{
    public GetCongregationByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
