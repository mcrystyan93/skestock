using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Statistics.Queries.GetClassStockByCategory;

public class GetClassStockByCategoryQueryValidator : AbstractValidator<GetClassStockByCategoryQuery>
{
    public GetClassStockByCategoryQueryValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
