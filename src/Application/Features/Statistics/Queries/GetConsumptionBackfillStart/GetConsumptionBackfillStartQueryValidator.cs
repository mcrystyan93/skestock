namespace skestock.Application.Features.Statistics.Queries.GetConsumptionBackfillStart;

public sealed class GetConsumptionBackfillStartQueryValidator : AbstractValidator<GetConsumptionBackfillStartQuery>
{
    public GetConsumptionBackfillStartQueryValidator()
    {
        RuleFor(query => query.TimeZoneId)
            .NotEmpty()
            .Must(timeZoneId => TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
            .WithMessage("TimeZoneId must be a known time zone.");
    }
}
