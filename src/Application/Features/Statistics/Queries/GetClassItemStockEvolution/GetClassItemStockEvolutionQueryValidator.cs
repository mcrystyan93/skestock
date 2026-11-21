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
            .When(query => query.ItemId.HasValue)
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(query => query.LocationId)
            .NotEmpty()
            .When(query => query.LocationId.HasValue)
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
