using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SupplyLists;

// Validation shared by create/update. excludeId is the list being updated (null on create) so
// its own name doesn't count as a duplicate.
public abstract class SupplyListPayloadValidator<TCommand> : AbstractValidator<TCommand>
    where TCommand : ISupplyListPayload
{
    private const int NameMaxLength = 200;
    private const int NoteMaxLength = 1000;
    private const int UnitMaxLength = 50;
    private const int LineNotesMaxLength = 500;
    private const int MinIntervalWeeks = 2;
    private const int MaxIntervalWeeks = 52;

    protected SupplyListPayloadValidator(IApplicationDbContext dbContext, Func<TCommand, Guid?> excludeId)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(NameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength)
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .MustAsync(async (command, ct) =>
                        !await SupplyListNameValidation.IsNameTakenAsync(dbContext, excludeId(command), command.Name, ct))
                    .WithName(nameof(ISupplyListPayload.Name))
                    .WithMessage("A supply list with this name already exists")
                    .WithErrorCode(ValidationErrorCodes.DuplicateName);
            });

        RuleFor(x => x.Note)
            .MaximumLength(NoteMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.Frequency)
            .IsInEnum()
            .WithErrorCode(ValidationErrorCodes.InvalidEnum);

        RuleFor(x => x.IntervalWeeks)
            .NotNull()
            .InclusiveBetween(MinIntervalWeeks, MaxIntervalWeeks)
            .WithErrorCode(ValidationErrorCodes.Between)
            .When(x => x.Frequency == SupplyListFrequency.EveryXWeeks);

        RuleFor(x => x.IntervalWeeks)
            .Null()
            .WithMessage("IntervalWeeks can only be set when the frequency is EveryXWeeks")
            .When(x => x.Frequency != SupplyListFrequency.EveryXWeeks);

        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.ItemId).Distinct().Count() == lines.Count)
            .WithMessage("An item can appear only once in a supply list")
            .WithErrorCode(ValidationErrorCodes.DuplicateName);

        RuleForEach(x => x.Lines)
            .ChildRules(line =>
            {
                line.RuleFor(l => l.ItemId)
                    .NotEmpty()
                    .WithErrorCode(ValidationErrorCodes.Required)
                    .MustAsync((itemId, ct) => dbContext.Items.AsNoTracking().AnyAsync(i => i.Id == itemId, ct))
                    .WithMessage("The referenced item does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);

                line.RuleFor(l => l.Quantity)
                    .GreaterThan(0)
                    .WithErrorCode(ValidationErrorCodes.GreaterThan);

                line.RuleFor(l => l.Unit)
                    .MaximumLength(UnitMaxLength)
                    .WithErrorCode(ValidationErrorCodes.MaxLength);

                line.RuleFor(l => l.Notes)
                    .MaximumLength(LineNotesMaxLength)
                    .WithErrorCode(ValidationErrorCodes.MaxLength);
            });
    }
}
