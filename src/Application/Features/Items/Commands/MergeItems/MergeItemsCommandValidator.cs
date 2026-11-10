using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Items.Commands.MergeItems;

public class MergeItemsCommandValidator : AbstractValidator<MergeItemsCommand>
{
    public MergeItemsCommandValidator()
    {
        RuleFor(x => x.ItemIds)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.ItemIds)
            .Must(ids => ids.Distinct().Count() >= 2).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo)
            .When(x => x.ItemIds.Count > 0);

        RuleForEach(x => x.ItemIds)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x)
            .Must(x => x.ItemIds.Contains(x.PrimaryItemId)).WithErrorCode(ValidationErrorCodes.InvalidReference)
            .WithName(nameof(MergeItemsCommand.PrimaryItemId))
            .When(x => x.ItemIds.Count > 0);
    }
}
