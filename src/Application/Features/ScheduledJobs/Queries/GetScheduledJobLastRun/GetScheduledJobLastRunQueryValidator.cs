namespace skestock.Application.Features.ScheduledJobs.Queries.GetScheduledJobLastRun;

public sealed class GetScheduledJobLastRunQueryValidator
    : AbstractValidator<GetScheduledJobLastRunQuery>
{
    public GetScheduledJobLastRunQueryValidator()
    {
        RuleFor(query => query.JobName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
