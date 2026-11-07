using skestock.Application.Common.Errors;
using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertPlans.Commands.SendFruitDessertMessage;

public class SendFruitDessertMessageCommandValidator : AbstractValidator<SendFruitDessertMessageCommand>
{
    public SendFruitDessertMessageCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.EntryId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        // Informare is a PDF-only template; sending it over WhatsApp would write history.
        RuleFor(x => x.Type).Must(t => t is FruitDessertMessageType.Reminder or FruitDessertMessageType.Thanks).WithErrorCode(ValidationErrorCodes.Required);
    }
}
