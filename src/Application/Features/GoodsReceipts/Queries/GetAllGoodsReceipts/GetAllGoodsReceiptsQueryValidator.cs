using skestock.Application.Common.Errors;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceipts;

public class GetAllGoodsReceiptsQueryValidator : AbstractValidator<GetAllGoodsReceiptsQuery>
{
    private static readonly IKeysetSortConfiguration<GoodsReceipt> SortConfiguration = new GoodsReceiptSortConfiguration();

    private static readonly IReadOnlySet<string> AllowedSortKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "receivedAt",
        "createdDate",
        "lastModifiedDate",
        "id"
    };

    private static readonly IReadOnlySet<string> AllowedDirections = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "asc",
        "ascend",
        "desc",
        "descend"
    };

    public GetAllGoodsReceiptsQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationConstants.DEFAULT_PAGE_SIZE)
            .WithMessage($"PageSize must be between 1 and {PaginationConstants.DEFAULT_PAGE_SIZE}")
            .WithErrorCode(ValidationErrorCodes.Between);

        RuleForEach(x => x.Sort)
            .Must(s => AllowedSortKeys.Contains(s.Key))
            .WithMessage($"Sort key must be one of: {string.Join(", ", AllowedSortKeys)}")
            .WithErrorCode(ValidationErrorCodes.InvalidSortKey)
            .DependentRules(() =>
            {
                RuleFor(x => x.Sort)
                    .ForEach(r => r
                        .Must(s => AllowedDirections.Contains(s.Value ?? ""))
                        .WithMessage($"Sort direction must be one of: {string.Join(", ", AllowedDirections)}")
                        .WithErrorCode(ValidationErrorCodes.InvalidSortDirection));
            });

        RuleFor(x => x.Cursor)
            .Must(ValidateCursor)
            .When(x => !string.IsNullOrWhiteSpace(x.Cursor))
            .WithMessage("Cursor is malformed or invalid")
            .WithErrorCode(ValidationErrorCodes.InvalidCursor);

        RuleFor(x => x)
            .Must(HaveCursorMatchingRequestedSort)
            .When(x => !string.IsNullOrWhiteSpace(x.Cursor) && ValidateCursor(x.Cursor))
            .WithMessage("Cursor was issued for a different sort order; request a new cursor for the current sort")
            .WithErrorCode(ValidationErrorCodes.CursorSortMismatch);
    }

    private static bool HaveCursorMatchingRequestedSort(GetAllGoodsReceiptsQuery query)
    {
        var cursorState = CursorCodec<GoodsReceipt>.Decode(query.Cursor);
        if (cursorState is null)
            return true; // Malformed cursor is already reported by the InvalidCursor rule above.

        var effectiveSort = DynamicSortBuilder<GoodsReceipt>.BuildEffectiveSort(query.Sort, SortConfiguration);
        return CursorCodec<GoodsReceipt>.MatchesSort(cursorState, effectiveSort);
    }

    private static bool ValidateCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return true;

        var decoded = CursorCodec<GoodsReceipt>.Decode(cursor);
        return decoded is not null;
    }
}
