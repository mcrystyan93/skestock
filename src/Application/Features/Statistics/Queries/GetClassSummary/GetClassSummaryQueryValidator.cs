using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Statistics.Queries.GetClassSummary;

public class GetClassSummaryQueryValidator : AbstractValidator<GetClassSummaryQuery>
{
    public GetClassSummaryQueryValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
