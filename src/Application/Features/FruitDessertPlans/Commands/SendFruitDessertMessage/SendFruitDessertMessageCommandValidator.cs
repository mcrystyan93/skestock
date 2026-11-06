using skestock.Application.Common.Errors;

namespace skestock.Application.Features.FruitDessertPlans.Commands.SendFruitDessertMessage;

public class SendFruitDessertMessageCommandValidator : AbstractValidator<SendFruitDessertMessageCommand>
{
    public SendFruitDessertMessageCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.EntryId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.Type).IsInEnum().WithErrorCode(ValidationErrorCodes.Required);
    }
}
