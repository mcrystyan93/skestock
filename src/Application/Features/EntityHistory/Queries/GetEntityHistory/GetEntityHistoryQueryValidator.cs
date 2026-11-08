using skestock.Application.Common.Errors;
using skestock.Application.Common.Models;

namespace skestock.Application.Features.EntityHistory.Queries.GetEntityHistory;

public sealed class GetEntityHistoryQueryValidator : AbstractValidator<GetEntityHistoryQuery>
{
    public const int MaxOffset = 1_000_000;

    public GetEntityHistoryQueryValidator()
    {
        RuleFor(x => x.Entity)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(128).WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.Id)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(450).WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationConstants.DEFAULT_PAGE_SIZE)
            .WithErrorCode(ValidationErrorCodes.Between);

        // The cursor is the next offset: non-negative and capped so skip + count cannot overflow int.
        RuleFor(x => x.Cursor)
            .Must(cursor => int.TryParse(cursor, out var offset) && offset is >= 0 and <= MaxOffset)
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithErrorCode(ValidationErrorCodes.InvalidCursor);
    }
}
