using System.Text.RegularExpressions;
using skestock.Application.Common.Errors;

namespace skestock.Application.Features.FruitDessertTemplates.Commands.UpdateMessageTemplate;

public partial class UpdateMessageTemplateCommandValidator : AbstractValidator<UpdateMessageTemplateCommand>
{
    public const int NameMaxLength = 100;
    public const int BodyMaxLength = 4000;
    public const int RenderedMaxLength = 2000;

    public UpdateMessageTemplateCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(NameMaxLength).WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.Body)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required)
            .Must(b => WhatsAppFormatter.Sanitize(b).Length <= BodyMaxLength).WithErrorCode(ValidationErrorCodes.MaxLength)
            .DependentRules(() =>
            {
                RuleFor(x => x.Body)
                    .Custom((body, context) =>
                    {
                        var converted = WhatsAppFormatter.ToWhatsApp(body);

                        var unknown = PlaceholderRegex().Matches(converted)
                            .Select(m => m.Value)
                            .Where(t => !MessagePlaceholders.IsKnown(t))
                            .Distinct()
                            .ToList();
                        if (unknown.Count > 0)
                        {
                            context.AddFailure(
                                $"Mesajul conține variabile necunoscute: {string.Join(", ", unknown)}. " +
                                $"Variabile permise: {string.Join(", ", MessagePlaceholders.All.Select(p => p.Token))}.");
                            return;
                        }

                        // Worst case: every placeholder expands to its column limit.
                        var worstCase = PlaceholderRegex().Replace(converted, m => new string('x', MessagePlaceholders.All.First(p => p.Token == m.Value).MaxLength));
                        if (worstCase.Length > RenderedMaxLength)
                        {
                            context.AddFailure(
                                $"Mesajul final poate depăși {RenderedMaxLength} de caractere după înlocuirea variabilelor.");
                        }
                    });
            });
    }

    [GeneratedRegex(@"\{[A-Za-z]+\}")]
    private static partial Regex PlaceholderRegex();
}
