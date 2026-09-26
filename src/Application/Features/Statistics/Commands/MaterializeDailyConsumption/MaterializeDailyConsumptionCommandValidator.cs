namespace skestock.Application.Features.Statistics.Commands.MaterializeDailyConsumption;

public sealed class MaterializeDailyConsumptionCommandValidator
    : AbstractValidator<MaterializeDailyConsumptionCommand>
{
    public MaterializeDailyConsumptionCommandValidator()
    {
        RuleFor(command => command.TimeZoneId)
            .NotEmpty()
            .Must(BeKnownTimeZone)
            .WithMessage("TimeZoneId must be a known time zone.");

        RuleFor(command => command.ToDate)
            .GreaterThanOrEqualTo(command => command.FromDate)
            .WithMessage("ToDate must be on or after FromDate.");

        RuleFor(command => command)
            .Must(command => command.ToDate.DayNumber - command.FromDate.DayNumber
                < MaterializeDailyConsumptionCommand.MaxDays)
            .When(command => command.ToDate >= command.FromDate)
            .WithName(nameof(MaterializeDailyConsumptionCommand.ToDate))
            .WithMessage(
                $"The date range cannot exceed {MaterializeDailyConsumptionCommand.MaxDays} days.");
    }

    private static bool BeKnownTimeZone(string timeZoneId) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
