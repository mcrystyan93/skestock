namespace skestock.Application.Features.ScheduledJobs.Commands.RecordScheduledJobRun;

public sealed class RecordScheduledJobRunCommandValidator
    : AbstractValidator<RecordScheduledJobRunCommand>
{
    public RecordScheduledJobRunCommandValidator()
    {
        RuleFor(command => command.JobName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.AttemptedAtUtc)
            .Must(timestamp => timestamp.Offset == TimeSpan.Zero)
            .WithMessage("AttemptedAtUtc must be UTC.");

        RuleFor(command => command.AttemptCount)
            .GreaterThan(0);

        RuleFor(command => command.SucceededAtUtc)
            .Must(timestamp => !timestamp.HasValue || timestamp.Value.Offset == TimeSpan.Zero)
            .WithMessage("SucceededAtUtc must be UTC.");

        RuleFor(command => command.NextRetryAtUtc)
            .Must(timestamp => !timestamp.HasValue || timestamp.Value.Offset == TimeSpan.Zero)
            .WithMessage("NextRetryAtUtc must be UTC.");

        RuleFor(command => command.Error)
            .MaximumLength(4000);
    }
}
