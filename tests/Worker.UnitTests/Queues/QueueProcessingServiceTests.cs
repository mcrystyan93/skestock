using Azure.Storage.Queues;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Categories.Commands.ProcessCategoryImportBatch;
using skestock.Application.Features.GoodsReceipts.Commands.ProcessGoodsReceiptImport;
using skestock.Application.Features.Items.Commands.ProcessItemImportBatch;
using Worker.Queues;

namespace Worker.UnitTests.Queues;

[TestFixture]
public sealed class QueueProcessingServiceTests
{
    [TestCase(QueueProcessorKind.Category)]
    [TestCase(QueueProcessorKind.GoodsReceipt)]
    [TestCase(QueueProcessorKind.Item)]
    public async Task Successful_message_is_dispatched_audited_and_deleted(QueueProcessorKind kind)
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var harness = CreateHarness(kind, messageId, userId);
        harness.ConfigureSender((_, _) => ValueTask.FromResult<object?>(Result.Ok()));

        await RunProcessorAsync(kind, harness);

        harness.SentRequests.Count.ShouldBe(1);
        harness.SentRequests[0].GetType().ShouldBe(GetExpectedCommandType(kind));
        harness.ObservedUserIds.ShouldBe([userId]);
        harness.DbContext.ProcessedMessages.Any(message => message.Id == messageId).ShouldBeTrue();
        harness.DeleteCount.ShouldBe(1);
        harness.PoisonMessages.ShouldBeEmpty();
        harness.ReceiveCalls.ShouldHaveSingleItem();
        harness.ReceiveCalls[0].MaxMessages.ShouldBe(10);
        harness.ReceiveCalls[0].VisibilityTimeout.ShouldBe(GetExpectedVisibilityTimeout(kind));
        harness.MainQueueClient.Verify(
            client => client.CreateIfNotExistsAsync(
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestCase(QueueProcessorKind.Category)]
    [TestCase(QueueProcessorKind.GoodsReceipt)]
    [TestCase(QueueProcessorKind.Item)]
    public async Task Duplicate_message_is_deleted_without_dispatch(QueueProcessorKind kind)
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(kind, messageId);
        harness.AddProcessedMessage(messageId);

        await RunProcessorAsync(kind, harness);

        harness.SentRequests.ShouldBeEmpty();
        harness.DeleteCount.ShouldBe(1);
        harness.PoisonMessages.ShouldBeEmpty();
        harness.DbContext.ProcessedMessages.Count(message => message.Id == messageId).ShouldBe(1);
    }

    [TestCase(QueueProcessorKind.Category)]
    [TestCase(QueueProcessorKind.GoodsReceipt)]
    [TestCase(QueueProcessorKind.Item)]
    public async Task Failed_result_is_poisoned_and_deleted(QueueProcessorKind kind)
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(kind, messageId);
        harness.ConfigureSender((_, _) => ValueTask.FromResult<object?>(Result.Fail("The import no longer exists.")));

        await RunProcessorAsync(kind, harness);

        harness.SentRequests.Count.ShouldBe(1);
        harness.DbContext.ProcessedMessages.ShouldBeEmpty();
        harness.PoisonMessages.ShouldHaveSingleItem();
        harness.PoisonMessages[0].ShouldBe(harness.SourceMessageText);
        harness.DeleteCount.ShouldBe(1);
    }

    [TestCase(QueueProcessorKind.Category)]
    [TestCase(QueueProcessorKind.GoodsReceipt)]
    [TestCase(QueueProcessorKind.Item)]
    public async Task Permanent_exception_is_poisoned_without_retry(QueueProcessorKind kind)
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(kind, messageId);
        harness.ConfigureSender((_, _) =>
            ValueTask.FromException<object?>(new ArgumentException("The request is invalid.")));

        await RunProcessorAsync(kind, harness);

        harness.SentRequests.Count.ShouldBe(1);
        harness.PoisonMessages.ShouldHaveSingleItem();
        harness.DeleteCount.ShouldBe(1);
        harness.DbContext.ProcessedMessages.ShouldBeEmpty();
    }

    [TestCase(QueueProcessorKind.Category)]
    [TestCase(QueueProcessorKind.GoodsReceipt)]
    [TestCase(QueueProcessorKind.Item)]
    public async Task Transient_exception_with_retries_remaining_keeps_source_message(QueueProcessorKind kind)
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(kind, messageId, dequeueCount: 1);
        harness.ConfigureSender((_, _) =>
        {
            harness.Stop();
            return ValueTask.FromException<object?>(new InvalidOperationException("Temporary dependency failure."));
        });

        await RunProcessorAsync(kind, harness);

        harness.SentRequests.Count.ShouldBe(1);
        harness.PoisonMessages.ShouldBeEmpty();
        harness.DeleteCount.ShouldBe(0);
        harness.DbContext.ProcessedMessages.ShouldBeEmpty();
    }

    [TestCase(QueueProcessorKind.Category)]
    [TestCase(QueueProcessorKind.GoodsReceipt)]
    [TestCase(QueueProcessorKind.Item)]
    public async Task Transient_exception_at_retry_limit_is_poisoned_and_deleted(QueueProcessorKind kind)
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(kind, messageId, dequeueCount: 5);
        harness.ConfigureSender((_, _) =>
            ValueTask.FromException<object?>(new InvalidOperationException("Temporary dependency failure.")));

        await RunProcessorAsync(kind, harness);

        harness.SentRequests.Count.ShouldBe(1);
        harness.PoisonMessages.ShouldHaveSingleItem();
        harness.DeleteCount.ShouldBe(1);
    }

    [TestCase(QueueProcessorKind.Category)]
    [TestCase(QueueProcessorKind.GoodsReceipt)]
    [TestCase(QueueProcessorKind.Item)]
    public async Task Malformed_message_is_preserved_in_raw_poison_queue(QueueProcessorKind kind)
    {
        using var harness = CreateHarnessWithRawText(kind, "not-base64");

        await RunProcessorAsync(kind, harness);

        harness.SentRequests.ShouldBeEmpty();
        harness.PoisonMessages.ShouldHaveSingleItem();
        harness.PoisonMessages[0].ShouldBe("not-base64");
        harness.DeleteCount.ShouldBe(1);
    }

    [TestCase(QueueProcessorKind.Category)]
    [TestCase(QueueProcessorKind.GoodsReceipt)]
    [TestCase(QueueProcessorKind.Item)]
    public async Task Unknown_message_type_at_retry_limit_is_poisoned_and_deleted(QueueProcessorKind kind)
    {
        var envelope = new skestock.Domain.Queues.MessageEnvelope
        {
            MessageId = Guid.NewGuid(),
            Type = "Missing.Message.Type, Missing.Assembly",
            Payload = "{}"
        };
        using var harness = CreateHarness(kind, QueueMessageFactory.CreateRaw(envelope, dequeueCount: 5));

        await RunProcessorAsync(kind, harness);

        harness.SentRequests.ShouldBeEmpty();
        harness.PoisonMessages.ShouldHaveSingleItem();
        harness.DeleteCount.ShouldBe(1);
    }

    [TestCase(QueueProcessorKind.Category)]
    [TestCase(QueueProcessorKind.Item)]
    public async Task Import_already_in_progress_is_acknowledged_without_poisoning(QueueProcessorKind kind)
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(kind, messageId);
        harness.ConfigureSender((_, _) =>
            ValueTask.FromException<object?>(new ImportBatchProcessingInProgressException(Guid.NewGuid())));

        await RunProcessorAsync(kind, harness);

        harness.SentRequests.Count.ShouldBe(1);
        harness.PoisonMessages.ShouldBeEmpty();
        harness.DeleteCount.ShouldBe(1);
        harness.DbContext.ProcessedMessages.ShouldBeEmpty();
    }

    private static QueueProcessingTestHarness CreateHarness(
        QueueProcessorKind kind,
        Guid messageId,
        Guid? userId = null,
        int dequeueCount = 1) =>
        new(kind, QueueMessageFactory.Create(kind, messageId, userId, dequeueCount));

    private static QueueProcessingTestHarness CreateHarness(
        QueueProcessorKind kind,
        Azure.Storage.Queues.Models.QueueMessage message) =>
        new(kind, message);

    private static QueueProcessingTestHarness CreateHarnessWithRawText(QueueProcessorKind kind, string messageText) =>
        new(kind, QueueMessageFactory.CreateRawText(messageText));

    private static async Task RunProcessorAsync(
        QueueProcessorKind kind,
        QueueProcessingTestHarness harness)
    {
        switch (kind)
        {
            case QueueProcessorKind.Category:
                await new TestableCategoryProcessor(
                    harness.QueueServiceClient.Object,
                    NullLogger<CategoryImportBatchQueueProcessingService>.Instance,
                    harness.ScopeFactory).RunAsync(harness.StopToken);
                break;
            case QueueProcessorKind.GoodsReceipt:
                await new TestableGoodsReceiptProcessor(
                    harness.QueueServiceClient.Object,
                    NullLogger<GoodsReceiptImportQueueProcessingService>.Instance,
                    harness.ScopeFactory).RunAsync(harness.StopToken);
                break;
            case QueueProcessorKind.Item:
                await new TestableItemProcessor(
                    harness.QueueServiceClient.Object,
                    NullLogger<ItemImportQueueProcessingService>.Instance,
                    harness.ScopeFactory).RunAsync(harness.StopToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static Type GetExpectedCommandType(QueueProcessorKind kind) =>
        kind switch
        {
            QueueProcessorKind.Category => typeof(ProcessCategoryImportBatchCommand),
            QueueProcessorKind.GoodsReceipt => typeof(ProcessGoodsReceiptImportCommand),
            QueueProcessorKind.Item => typeof(ProcessItemImportBatchCommand),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    private static TimeSpan GetExpectedVisibilityTimeout(QueueProcessorKind kind) =>
        kind == QueueProcessorKind.GoodsReceipt
            ? TimeSpan.FromSeconds(30)
            : TimeSpan.FromSeconds(1200);

    private sealed class TestableCategoryProcessor(
        QueueServiceClient queueServiceClient,
        ILogger<CategoryImportBatchQueueProcessingService> logger,
        IServiceScopeFactory scopeFactory)
        : CategoryImportBatchQueueProcessingService(queueServiceClient, logger, scopeFactory)
    {
        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }

    private sealed class TestableGoodsReceiptProcessor(
        QueueServiceClient queueServiceClient,
        ILogger<GoodsReceiptImportQueueProcessingService> logger,
        IServiceScopeFactory scopeFactory)
        : GoodsReceiptImportQueueProcessingService(queueServiceClient, logger, scopeFactory)
    {
        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }

    private sealed class TestableItemProcessor(
        QueueServiceClient queueServiceClient,
        ILogger<ItemImportQueueProcessingService> logger,
        IServiceScopeFactory scopeFactory)
        : ItemImportQueueProcessingService(queueServiceClient, logger, scopeFactory)
    {
        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }
}
