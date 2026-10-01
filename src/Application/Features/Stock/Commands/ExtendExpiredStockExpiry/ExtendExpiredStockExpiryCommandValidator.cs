using skestock.Application.Common.Errors;

namespace skestock.Application.Features.Stock.Commands.ExtendExpiredStockExpiry;

public class ExtendExpiredStockExpiryCommandValidator : AbstractValidator<ExtendExpiredStockExpiryCommand>
{
    public ExtendExpiredStockExpiryCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.ItemId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.LocationId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.ExtensionDays)
            .InclusiveBetween(1, ExtendExpiredStockExpiryCommand.MaxExtensionDays)
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
