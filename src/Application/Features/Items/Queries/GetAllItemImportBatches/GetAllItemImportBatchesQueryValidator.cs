using skestock.Application.Common.Errors;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Items.Queries.GetAllItemImportBatches;

public sealed class GetAllItemImportBatchesQueryValidator : AbstractValidator<GetAllItemImportBatchesQuery>
{
    private static readonly IKeysetSortConfiguration<ItemImportBatch> SortConfiguration =
        new ItemImportBatchSortConfiguration();

    private static readonly IReadOnlySet<string> AllowedSortKeys =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "uploadedAt",
            "createdDate",
            "lastModifiedDate",
            "id"
        };

    private static readonly IReadOnlySet<string> AllowedDirections =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "asc",
            "ascend",
            "desc",
            "descend"
        };

    public GetAllItemImportBatchesQueryValidator()
    {
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PaginationConstants.DEFAULT_PAGE_SIZE)
            .WithMessage($"PageSize must be between 1 and {PaginationConstants.DEFAULT_PAGE_SIZE}")
            .WithErrorCode(ValidationErrorCodes.Between);

        RuleForEach(query => query.Sort)
            .Must(sort => AllowedSortKeys.Contains(sort.Key))
            .WithMessage($"Sort key must be one of: {string.Join(", ", AllowedSortKeys)}")
            .WithErrorCode(ValidationErrorCodes.InvalidSortKey)
            .DependentRules(() =>
            {
                RuleFor(query => query.Sort)
                    .ForEach(rule => rule
                        .Must(sort => AllowedDirections.Contains(sort.Value ?? ""))
                        .WithMessage($"Sort direction must be one of: {string.Join(", ", AllowedDirections)}")
                        .WithErrorCode(ValidationErrorCodes.InvalidSortDirection));
            });

        RuleFor(query => query.Cursor)
            .Must(ValidateCursor)
            .When(query => !string.IsNullOrWhiteSpace(query.Cursor))
            .WithMessage("Cursor is malformed or invalid")
            .WithErrorCode(ValidationErrorCodes.InvalidCursor);

        RuleFor(query => query)
            .Must(HaveCursorMatchingRequestedSort)
            .When(query => !string.IsNullOrWhiteSpace(query.Cursor) && ValidateCursor(query.Cursor))
            .WithMessage("Cursor was issued for a different sort order; request a new cursor for the current sort")
            .WithErrorCode(ValidationErrorCodes.CursorSortMismatch);
    }

    private static bool HaveCursorMatchingRequestedSort(GetAllItemImportBatchesQuery query)
    {
        var cursorState = CursorCodec<ItemImportBatch>.Decode(query.Cursor);
        if (cursorState is null)
            return true;

        var effectiveSort = DynamicSortBuilder<ItemImportBatch>.BuildEffectiveSort(query.Sort, SortConfiguration);
        return CursorCodec<ItemImportBatch>.MatchesSort(cursorState, effectiveSort);
    }

    private static bool ValidateCursor(string? cursor) =>
        string.IsNullOrWhiteSpace(cursor) || CursorCodec<ItemImportBatch>.Decode(cursor) is not null;
}
