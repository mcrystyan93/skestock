using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Statistics.Queries.GetClassItemStockEvolution;

public class GetClassItemStockEvolutionQueryValidator : AbstractValidator<GetClassItemStockEvolutionQuery>
{
    public GetClassItemStockEvolutionQueryValidator()
    {
        RuleFor(query => query.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(query => query.ItemId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
