using skestock.Application.Common.Errors;
using skestock.Application.Common.Keyset;
using skestock.Application.Common.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Categories.Queries.GetAllCategoryImportBatches;

public class GetAllCategoryImportBatchesQueryValidator : AbstractValidator<GetAllCategoryImportBatchesQuery>
{
    private static readonly IKeysetSortConfiguration<CategoryImportBatch> SortConfiguration =
        new CategoryImportBatchSortConfiguration();

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

    public GetAllCategoryImportBatchesQueryValidator()
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
            .Must(cursor => string.IsNullOrWhiteSpace(cursor) ||
                            CursorCodec<CategoryImportBatch>.Decode(cursor) is not null)
            .When(query => !string.IsNullOrWhiteSpace(query.Cursor))
            .WithErrorCode(ValidationErrorCodes.InvalidCursor);

        RuleFor(query => query)
            .Must(HaveCursorMatchingRequestedSort)
            .When(query => !string.IsNullOrWhiteSpace(query.Cursor) &&
                           CursorCodec<CategoryImportBatch>.Decode(query.Cursor) is not null)
            .WithErrorCode(ValidationErrorCodes.CursorSortMismatch);
    }

    private static bool HaveCursorMatchingRequestedSort(GetAllCategoryImportBatchesQuery query)
    {
        var cursorState = CursorCodec<CategoryImportBatch>.Decode(query.Cursor);
        if (cursorState is null)
            return true;

        var effectiveSort = DynamicSortBuilder<CategoryImportBatch>.BuildEffectiveSort(
            query.Sort, SortConfiguration);
        return CursorCodec<CategoryImportBatch>.MatchesSort(cursorState, effectiveSort);
    }
}
