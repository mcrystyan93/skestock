namespace skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;

public sealed class MaterializePurchaseStatisticsCommandValidator
    : AbstractValidator<MaterializePurchaseStatisticsCommand>
{
    public MaterializePurchaseStatisticsCommandValidator()
    {
        RuleFor(command => command.TimeZoneId)
            .NotEmpty()
            .Must(timeZoneId => TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
            .WithMessage("TimeZoneId must be a known time zone.");
    }
}
