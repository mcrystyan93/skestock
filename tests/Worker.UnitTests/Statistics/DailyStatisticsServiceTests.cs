using FluentResults;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ScheduledJobs.Commands.RecordScheduledJobRun;
using skestock.Application.Features.ScheduledJobs.Models;
using skestock.Application.Features.ScheduledJobs.Queries.GetScheduledJobLastRun;
using skestock.Application.Features.Statistics.Commands.MaterializeDailyConsumption;
using skestock.Domain.Enums;
using Worker.Services;
using Worker.Statistics;
using SharedServices = skestock.Shared.Services;

namespace Worker.UnitTests.Statistics;

[TestFixture]
public sealed class DailyStatisticsServiceTests
{
    [Test]
    public async Task Startup_runs_a_missed_job_once()
    {
        using var cancellation = new CancellationTokenSource();
        var sender = CreateSender(cancellation, out var recordedRuns);
        using var provider = CreateServiceProvider(sender);
        var lockMock = CreateLockMock();
        var testService = CreateService(
            provider,
            lockMock.Object,
            new FakeTimeProvider(new DateTimeOffset(2026, 9, 25, 17, 0, 0, TimeSpan.Zero)),
            out _);

        await testService.RunAsync(cancellation.Token);

        testService.JobAttempts.ShouldBe(1);
        recordedRuns.Count(run => run.SucceededAtUtc.HasValue).ShouldBe(1);
        lockMock.Verify(
            distributedLock => distributedLock.TryAcquireAsync(
                SharedServices.DailyStatisticsLockKey,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Job_materializes_daily_consumption_for_the_local_range()
    {
        using var cancellation = new CancellationTokenSource();
        var sender = CreateSender(cancellation, out var recordedRuns);
        MaterializeDailyConsumptionCommand? sentCommand = null;
        sender
            .Setup(mediator => mediator.Send(
                It.IsAny<MaterializeDailyConsumptionCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<int>>, CancellationToken>(
                (request, _) => sentCommand = (MaterializeDailyConsumptionCommand)request)
            .Returns(new ValueTask<Result<int>>(Result.Ok(3)));
        using var provider = CreateServiceProvider(sender);
        var testService = CreateService(
            provider,
            CreateLockMock().Object,
            new FakeTimeProvider(new DateTimeOffset(2026, 9, 25, 18, 0, 0, TimeSpan.Zero)),
            out _);
        testService.UseRealJob = true;

        await testService.RunAsync(cancellation.Token);

        sentCommand.ShouldNotBeNull();
        sentCommand.TimeZoneId.ShouldBe("Europe/Bucharest");
        sentCommand.ToDate.ShouldBe(new DateOnly(2026, 9, 25));
        sentCommand.FromDate.ShouldBe(new DateOnly(2026, 8, 26));
        recordedRuns.Count(run => run.SucceededAtUtc.HasValue).ShouldBe(1);
    }

    [Test]
    public async Task Lock_not_acquired_skips_the_job()
    {
        using var cancellation = new CancellationTokenSource();
        var sender = CreateSender(cancellation, out _);
        using var provider = CreateServiceProvider(sender);
        var lockMock = new Mock<IDistributedLock>(MockBehavior.Strict);
        lockMock
            .Setup(distributedLock => distributedLock.TryAcquireAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => cancellation.Cancel())
            .ReturnsAsync((IAsyncDisposable?)null);
        var testService = CreateService(
            provider,
            lockMock.Object,
            new FakeTimeProvider(new DateTimeOffset(2026, 9, 25, 17, 0, 0, TimeSpan.Zero)),
            out _);

        await testService.RunAsync(cancellation.Token);

        testService.JobAttempts.ShouldBe(0);
        testService.Delays.ShouldHaveSingleItem();
        testService.Delays[0].ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task Job_retries_three_times_with_configured_backoff()
    {
        using var cancellation = new CancellationTokenSource();
        var sender = CreateSender(cancellation, out _);
        using var provider = CreateServiceProvider(sender);
        var lockMock = CreateLockMock();
        var testService = CreateService(
            provider,
            lockMock.Object,
            new FakeTimeProvider(new DateTimeOffset(2026, 9, 25, 17, 0, 0, TimeSpan.Zero)),
            out var service);
        service.FailuresBeforeSuccess = 3;

        await testService.RunAsync(cancellation.Token);

        service.JobAttempts.ShouldBe(4);
        service.Delays.ShouldBe(
        [
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15)
        ]);
    }

    [Test]
    public async Task Restart_after_failed_attempt_resumes_the_remaining_retries()
    {
        var state = new ScheduledJobState();
        using var firstCancellation = new CancellationTokenSource();
        var firstSender = CreateStatefulSender(state);
        using var firstProvider = CreateServiceProvider(firstSender);
        var firstService = CreateService(
            firstProvider,
            CreateLockMock().Object,
            new FakeTimeProvider(new DateTimeOffset(2026, 9, 25, 17, 0, 0, TimeSpan.Zero)),
            out var firstTestService);
        firstTestService.FailuresBeforeSuccess = 1;
        firstTestService.OnDelay = firstCancellation.Cancel;

        await firstTestService.RunAsync(firstCancellation.Token);

        firstTestService.JobAttempts.ShouldBe(1);
        state.LastRun!.LastError.ShouldBe("Temporary test failure.");

        using var secondCancellation = new CancellationTokenSource();
        var secondSender = CreateStatefulSender(state);
        using var secondProvider = CreateServiceProvider(secondSender);
        var secondService = CreateService(
            secondProvider,
            CreateLockMock().Object,
            new FakeTimeProvider(new DateTimeOffset(2026, 9, 25, 17, 2, 0, TimeSpan.Zero)),
            out var secondTestService);
        secondTestService.OnDelay = secondCancellation.Cancel;

        await secondTestService.RunAsync(secondCancellation.Token);

        secondTestService.JobAttempts.ShouldBe(1);
        state.LastRun.AttemptCount.ShouldBe(2);
        state.LastRun.LastSucceededAt.ShouldNotBeNull();
    }

    [Test]
    public async Task Restart_after_exhausted_failure_with_empty_message_does_not_start_a_new_retry_cycle()
    {
        var state = new ScheduledJobState();
        using var firstCancellation = new CancellationTokenSource();
        var firstSender = CreateStatefulSender(state);
        using var firstProvider = CreateServiceProvider(firstSender);
        var firstService = CreateService(
            firstProvider,
            CreateLockMock().Object,
            new FakeTimeProvider(new DateTimeOffset(2026, 9, 25, 17, 0, 0, TimeSpan.Zero)),
            out var firstTestService);
        firstTestService.FailuresBeforeSuccess = 4;
        firstTestService.FailureMessage = string.Empty;
        firstTestService.OnJobAttempt = attempt =>
        {
            if (attempt == 4)
            {
                firstCancellation.Cancel();
            }
        };

        await firstTestService.RunAsync(firstCancellation.Token);

        firstTestService.JobAttempts.ShouldBe(4);
        state.LastRun!.LastError.ShouldBeEmpty();

        using var secondCancellation = new CancellationTokenSource();
        var secondSender = CreateStatefulSender(state);
        using var secondProvider = CreateServiceProvider(secondSender);
        var secondService = CreateService(
            secondProvider,
            CreateLockMock().Object,
            new FakeTimeProvider(new DateTimeOffset(2026, 9, 25, 17, 2, 0, TimeSpan.Zero)),
            out var secondTestService);
        secondTestService.OnDelay = secondCancellation.Cancel;

        await secondTestService.RunAsync(secondCancellation.Token);

        secondTestService.JobAttempts.ShouldBe(0);
    }

    private static TestableDailyStatisticsService CreateService(
        ServiceProvider provider,
        IDistributedLock distributedLock,
        TimeProvider timeProvider,
        out TestableDailyStatisticsService testService)
    {
        testService = new TestableDailyStatisticsService(
            Options.Create(new DailyStatisticsOptions()),
            provider.GetRequiredService<IServiceScopeFactory>(),
            distributedLock,
            timeProvider,
            NullLogger<DailyStatisticsService>.Instance);
        return testService;
    }

    private static ServiceProvider CreateServiceProvider(Mock<ISender> sender)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ISender>(_ => sender.Object);
        services.AddScoped<AmbientUser>();
        services.AddScoped<IUser>(provider => provider.GetRequiredService<AmbientUser>());
        return services.BuildServiceProvider();
    }

    private static Mock<ISender> CreateSender(
        CancellationTokenSource cancellation,
        out List<RecordScheduledJobRunCommand> recordedRuns)
    {
        var runs = new List<RecordScheduledJobRunCommand>();
        recordedRuns = runs;
        var sender = new Mock<ISender>(MockBehavior.Strict);

        sender
            .Setup(mediator => mediator.Send(
                It.IsAny<GetScheduledJobLastRunQuery>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                new ValueTask<Result<ScheduledJobRunDto?>>(
                    Result.Ok<ScheduledJobRunDto?>(null)));

        sender
            .Setup(mediator => mediator.Send(
                It.IsAny<RecordScheduledJobRunCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result>, CancellationToken>(
                (request, _) =>
                {
                    var command = (RecordScheduledJobRunCommand)request;
                    runs.Add(command);
                    if (command.SucceededAtUtc.HasValue)
                    {
                        cancellation.Cancel();
                    }
                })
            .Returns(
                new ValueTask<Result>(
                    Result.Ok()));

        return sender;
    }

    private static Mock<ISender> CreateStatefulSender(ScheduledJobState state)
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);

        sender
            .Setup(mediator => mediator.Send(
                It.IsAny<GetScheduledJobLastRunQuery>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                () => new ValueTask<Result<ScheduledJobRunDto?>>(
                    Result.Ok<ScheduledJobRunDto?>(state.LastRun)));

        sender
            .Setup(mediator => mediator.Send(
                It.IsAny<RecordScheduledJobRunCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result>, CancellationToken>(
                (request, _) =>
                {
                    var command = (RecordScheduledJobRunCommand)request;
                    var previousRun = state.LastRun;
                    state.LastRun = new ScheduledJobRunDto
                    {
                        JobName = command.JobName,
                        Status = command.Status,
                        AttemptCount = command.AttemptCount,
                        NextRetryAt = command.NextRetryAtUtc,
                        LastAttemptAt = command.AttemptedAtUtc,
                        LastSucceededAt = command.SucceededAtUtc ?? previousRun?.LastSucceededAt,
                        LastError = command.Error
                    };
                })
            .Returns(
                new ValueTask<Result>(
                    Result.Ok()));

        return sender;
    }

    private static Mock<IDistributedLock> CreateLockMock()
    {
        var lockMock = new Mock<IDistributedLock>(MockBehavior.Strict);
        lockMock
            .Setup(distributedLock => distributedLock.TryAcquireAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => (IAsyncDisposable)new NoopLease());
        return lockMock;
    }

    private sealed class TestableDailyStatisticsService(
        IOptions<DailyStatisticsOptions> options,
        IServiceScopeFactory scopeFactory,
        IDistributedLock distributedLock,
        TimeProvider timeProvider,
        Microsoft.Extensions.Logging.ILogger<DailyStatisticsService> logger)
        : DailyStatisticsService(options, scopeFactory, distributedLock, timeProvider, logger)
    {
        public int JobAttempts { get; private set; }

        public bool UseRealJob { get; set; }

        public int FailuresBeforeSuccess { get; set; }

        public string FailureMessage { get; set; } = "Temporary test failure.";

        public List<TimeSpan> Delays { get; } = [];

        public Action? OnDelay { get; set; }

        public Action<int>? OnJobAttempt { get; set; }

        public Task RunAsync(CancellationToken cancellationToken) =>
            ExecuteAsync(cancellationToken);

        protected override Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            OnDelay?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        protected override Task ExecuteJobAsync(
            ISender sender,
            DailyStatisticsJobContext context,
            CancellationToken cancellationToken)
        {
            if (UseRealJob)
            {
                return base.ExecuteJobAsync(sender, context, cancellationToken);
            }

            JobAttempts++;
            OnJobAttempt?.Invoke(JobAttempts);
            if (JobAttempts <= FailuresBeforeSuccess)
            {
                throw new InvalidOperationException(FailureMessage);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ScheduledJobState
    {
        public ScheduledJobRunDto? LastRun { get; set; }
    }

    private sealed class NoopLease : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
