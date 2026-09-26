using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Statistics.Queries.GetItemsPurchaseHistory;

public class GetItemsPurchaseHistoryQueryValidator : AbstractValidator<GetItemsPurchaseHistoryQuery>
{
    public GetItemsPurchaseHistoryQueryValidator()
    {
        RuleFor(query => query.ItemIds)
            .NotNull()
            .Must(ids => ids.Count <= GetItemsPurchaseHistoryQuery.MaxItems)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .WithMessage($"At most {GetItemsPurchaseHistoryQuery.MaxItems} items can be requested.");

        RuleForEach(query => query.ItemIds)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
