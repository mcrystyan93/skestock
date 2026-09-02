using skestock.Application.Common.Errors;

namespace skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassSummary;

public class GetSchoolClassSummaryQueryValidator : AbstractValidator<GetSchoolClassSummaryQuery>
{
    public GetSchoolClassSummaryQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
