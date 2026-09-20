using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Domain.Queues;

namespace skestock.Application.FunctionalTests.Features.GoodsReceipts.Commands.ProcessGoodsReceiptImport;

public class OutboxWorkerIntegrationTests : TestBase
{
    [Test]
    public async Task CreateImport_PublishesOutboxMessageConsumedByWorker()
    {
        var prefix = $"FT{Guid.NewGuid():N}"[..10];
        var schoolClass = new SchoolClass
        {
            Name = $"{prefix}-Class",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1)
        };
        await TestApp.AddAsync(schoolClass);

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = $"{prefix}-receipt.pdf",
            BlobContainer = "app-files",
            BlobPath = $"imports/{prefix}/receipt.pdf",
            ContentType = "application/pdf",
            SizeBytes = 2048,
            Status = FileStatus.Completed
        };
        await TestApp.AddAsync(file);

        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile
        {
            IdentityId = userId!.Value,
            FirstName = "Staff",
            LastName = "Member"
        });

        var result = await TestApp.SendAsync(new CreateGoodsReceiptImportCommand
        {
            ClassId = schoolClass.Id,
            FileMetadataId = file.Id
        });

        result.IsSuccess.ShouldBeTrue();
        var importId = result.Value.Id;
        var outboxMessage = await TestApp.SingleOrDefaultAsync<OutboxMessage>(
            message => message.Payload.Contains(importId.ToString()));

        outboxMessage.ShouldNotBeNull();
        var outboxMessageId = outboxMessage!.Id;

        // Avoid an external OpenAI call while still letting the real Worker dispatch the command.
        // Assigning the fixture's terminal state directly avoids publishing the currently
        // unimplemented failure-notification handler during test setup.
        await MarkImportAsFailedAsync(importId);

        await WaitForProcessedMessageAsync(outboxMessageId, importId);

        var persistedOutboxMessage = await TestApp.FindAsync<OutboxMessage>(outboxMessageId);
        persistedOutboxMessage.ShouldNotBeNull();
        persistedOutboxMessage.ProcessedAtUtc.ShouldNotBeNull();
        persistedOutboxMessage.RetryCount.ShouldBe(0);

        var persistedImport = await TestApp.FindAsync<GoodsReceiptImport>(importId);
        persistedImport.ShouldNotBeNull();
        persistedImport.Status.ShouldBe(GoodsReceiptImportStatus.Failed);
        (await TestApp.FindAsync<ProcessedMessage>(outboxMessageId)).ShouldNotBeNull();
    }

    private static async Task MarkImportAsFailedAsync(Guid importId)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var import = await dbContext.GoodsReceiptImports
            .SingleAsync(candidate => candidate.Id == importId);

        import.Status = GoodsReceiptImportStatus.Failed;
        import.ErrorMessage = "Integration test terminal state.";
        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task WaitForProcessedMessageAsync(Guid messageId, Guid importId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        OutboxMessage? lastOutboxMessage = null;
        GoodsReceiptImport? lastImport = null;

        while (DateTime.UtcNow < deadline)
        {
            if (await TestApp.FindAsync<ProcessedMessage>(messageId) is not null)
                return;

            lastOutboxMessage = await TestApp.FindAsync<OutboxMessage>(messageId);
            lastImport = await TestApp.FindAsync<GoodsReceiptImport>(importId);
            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        Assert.Fail(
            $"Worker did not process outbox message {messageId} within 30 seconds. " +
            $"Outbox processed at: {lastOutboxMessage?.ProcessedAtUtc}; " +
            $"retry count: {lastOutboxMessage?.RetryCount}; error: {lastOutboxMessage?.Error}; " +
            $"Import status: {lastImport?.Status}; error: {lastImport?.ErrorMessage}");
    }
}
