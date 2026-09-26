using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Statistics.Queries.GetDailyConsumptionAverages;

public class GetDailyConsumptionAveragesQueryValidator : AbstractValidator<GetDailyConsumptionAveragesQuery>
{
    public GetDailyConsumptionAveragesQueryValidator()
    {
        RuleFor(query => query.ItemId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(query => query.LocationId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(query => query.CategoryId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
