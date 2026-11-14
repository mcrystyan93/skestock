using skestock.Application.Common.Errors;

namespace skestock.Application.Features.OrderLists.Queries.GetOrderCatalog;

public class GetOrderCatalogQueryValidator : AbstractValidator<GetOrderCatalogQuery>
{
    public GetOrderCatalogQueryValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
