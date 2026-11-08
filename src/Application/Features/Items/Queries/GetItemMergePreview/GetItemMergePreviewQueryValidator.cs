using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Items.Queries.GetItemMergePreview;

public class GetItemMergePreviewQueryValidator : AbstractValidator<GetItemMergePreviewQuery>
{
    public GetItemMergePreviewQueryValidator()
    {
        RuleFor(x => x.ItemIds)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.ItemIds)
            .Must(ids => ids.Distinct().Count() >= 2).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo)
            .When(x => x.ItemIds.Count > 0);

        RuleFor(x => x)
            .Must(x => x.ItemIds.Contains(x.PrimaryItemId)).WithErrorCode(ValidationErrorCodes.InvalidReference)
            .WithName(nameof(GetItemMergePreviewQuery.PrimaryItemId))
            .When(x => x.ItemIds.Count > 0);
    }
}
