using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Statistics.Queries.GetClassEvolution;

public class GetClassEvolutionQueryValidator : AbstractValidator<GetClassEvolutionQuery>
{
    public GetClassEvolutionQueryValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
