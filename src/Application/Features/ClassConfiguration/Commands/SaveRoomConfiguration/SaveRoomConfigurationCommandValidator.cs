using skestock.Application.Common.Errors;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveRoomConfiguration;

public sealed class SaveRoomConfigurationCommandValidator : AbstractValidator<SaveRoomConfigurationCommand>
{
    public SaveRoomConfigurationCommandValidator()
    {
        RuleFor(command => command.Room4SeatCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.Room4SeatCount).GreaterThanOrEqualTo(0)
            .When(command => command.Room4SeatCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
        RuleFor(command => command.Room1SeatCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.Room1SeatCount).GreaterThanOrEqualTo(0)
            .When(command => command.Room1SeatCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
        RuleFor(command => command.Room6SeatCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.Room6SeatCount).GreaterThanOrEqualTo(0)
            .When(command => command.Room6SeatCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
    }
}
