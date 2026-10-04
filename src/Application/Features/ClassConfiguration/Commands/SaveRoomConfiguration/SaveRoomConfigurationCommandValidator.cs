using skestock.Application.Common.Errors;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveRoomConfiguration;

public sealed class SaveRoomConfigurationCommandValidator : AbstractValidator<SaveRoomConfigurationCommand>
{
    private const int MaximumSeatCount = 200;

    public SaveRoomConfigurationCommandValidator()
    {
        RuleFor(command => command.Room4SeatCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.Room4SeatCount).GreaterThanOrEqualTo(0)
            .When(command => command.Room4SeatCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
        RuleFor(command => command.Room4SeatCount).LessThanOrEqualTo(MaximumSeatCount)
            .When(command => command.Room4SeatCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.LessThanOrEqualTo);
        RuleFor(command => command.Room2SeatCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.Room2SeatCount).GreaterThanOrEqualTo(0)
            .When(command => command.Room2SeatCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
        RuleFor(command => command.Room2SeatCount).LessThanOrEqualTo(MaximumSeatCount)
            .When(command => command.Room2SeatCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.LessThanOrEqualTo);
        RuleFor(command => command.Room6SeatCount).NotNull()
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(command => command.Room6SeatCount).GreaterThanOrEqualTo(0)
            .When(command => command.Room6SeatCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);
        RuleFor(command => command.Room6SeatCount).LessThanOrEqualTo(MaximumSeatCount)
            .When(command => command.Room6SeatCount.HasValue)
            .WithErrorCode(ValidationErrorCodes.LessThanOrEqualTo);
    }
}
