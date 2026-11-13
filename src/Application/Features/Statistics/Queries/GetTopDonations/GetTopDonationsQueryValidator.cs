using skestock.Application.Common.Errors;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Queries.GetTopDonations;

public class GetTopDonationsQueryValidator : AbstractValidator<GetTopDonationsQuery>
{
    public GetTopDonationsQueryValidator()
    {
        RuleFor(query => query.Scope)
            .IsInEnum()
            .WithErrorCode(ValidationErrorCodes.InvalidEnum);

        RuleFor(query => query.ClassId)
            .NotNull()
            .NotEqual(Guid.Empty)
            .When(query => query.Scope == PurchaseStatisticsScope.Class)
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(query => query.CategoryId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(query => query.Top)
            .InclusiveBetween(1, GetTopDonationsQuery.MaxTop)
            .WithErrorCode(ValidationErrorCodes.Between);
    }
}
