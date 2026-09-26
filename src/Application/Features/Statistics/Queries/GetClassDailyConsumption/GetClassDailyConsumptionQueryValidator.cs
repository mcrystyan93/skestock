using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Statistics.Queries.GetClassDailyConsumption;

public class GetClassDailyConsumptionQueryValidator : AbstractValidator<GetClassDailyConsumptionQuery>
{
    public GetClassDailyConsumptionQueryValidator()
    {
        RuleFor(query => query.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
