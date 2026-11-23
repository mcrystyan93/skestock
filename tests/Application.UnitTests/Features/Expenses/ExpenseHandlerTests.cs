using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Features.Expenses.Commands.CreateExpense;
using skestock.Application.Features.Expenses.Commands.CreateExpenseFromDocument;
using skestock.Application.Features.Expenses.Commands.DeleteExpense;
using skestock.Application.Features.Expenses.Commands.ProcessExpenseDocument;
using skestock.Application.Features.Expenses.Commands.RetryExpenseProcessing;
using skestock.Application.Features.Expenses.Commands.UpdateExpense;
using skestock.Application.Features.Expenses.Models;
using skestock.Application.Queues;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Entities.Expenses;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Enums;
using skestock.Shared;

namespace skestock.Application.UnitTests.Features.Expenses;

public class ExpenseHandlerTests
{
    private sealed class FakeUser(Guid? id) : IUser
    {
        public Guid? Id { get; } = id;
        public List<string>? Roles => null;
    }

    private static readonly Guid UserId = Guid.NewGuid();

    private static ExpenseTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ExpenseTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(SchoolClass Class, FileMetadata File)> SeedClassAndFileAsync(ExpenseTestDbContext context)
    {
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "receipt.jpg",
            BlobContainer = "app-files",
            BlobPath = "expenses/receipt.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            Status = FileStatus.Completed
        };
        context.SchoolClasses.Add(schoolClass);
        context.FileMetadata.Add(file);
        await context.SaveChangesAsync();
        return (schoolClass, file);
    }

    private static async Task<Expense> SeedDocumentExpenseAsync(ExpenseTestDbContext context)
    {
        var (schoolClass, file) = await SeedClassAndFileAsync(context);
        var expense = Expense.CreateFromDocument(schoolClass.Id, file.Id, UserId);
        expense.SchoolClass = schoolClass;
        expense.FileMetadata = file;
        context.Expenses.Add(expense);
        await context.SaveChangesAsync();
        return expense;
    }

    private static ProcessExpenseDocumentCommandHandler CreateProcessHandler(
        ExpenseTestDbContext context, Mock<IStockDocumentExtractionService> extraction, bool finalAttempt = false)
    {
        var blob = new Mock<IBlobStorageService>();
        blob.Setup(b => b.DownloadAsync(It.IsAny<DownloadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());
        return new ProcessExpenseDocumentCommandHandler(
            context, blob.Object, extraction.Object, new MessageDeliveryContext { IsFinalAttempt = finalAttempt },
            NullLogger<ProcessExpenseDocumentCommandHandler>.Instance);
    }

    [Test]
    public async Task CreateFromDocument_CreatesProcessingExpenseAndOutboxMessage()
    {
        await using var context = CreateContext();
        var (schoolClass, file) = await SeedClassAndFileAsync(context);
        var handler = new CreateExpenseFromDocumentCommandHandler(context, new FakeUser(UserId));

        var result = await handler.Handle(
            new CreateExpenseFromDocumentCommand { ClassId = schoolClass.Id, FileMetadataId = file.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ExpenseStatus.Processing);
        (await context.Expenses.CountAsync()).ShouldBe(1);
        var outbox = await context.OutboxMessages.SingleAsync();
        outbox.QueueName.ShouldBe(Services.ExpenseQueue);
    }

    [Test]
    public async Task Process_WhenExtractionSucceeds_FillsDataAndMarksProcessed()
    {
        await using var context = CreateContext();
        var expense = await SeedDocumentExpenseAsync(context);
        var extraction = new Mock<IStockDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<ExpenseExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExpenseExtractionResult
            {
                DocumentType = "invoice",
                SupplierName = "ACME SRL",
                DocumentNumber = "F-12",
                ExpenseDate = new DateOnly(2026, 10, 5),
                Currency = "ron",
                TotalAmount = 119m,
                VatAmount = 19m
            });

        var result = await CreateProcessHandler(context, extraction)
            .Handle(new ProcessExpenseDocumentCommand(expense.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var saved = await context.Expenses.SingleAsync();
        saved.Status.ShouldBe(ExpenseStatus.Processed);
        saved.Source.ShouldBe(ExpenseSource.Invoice);
        saved.SupplierName.ShouldBe("ACME SRL");
        saved.Currency.ShouldBe("RON");
        saved.TotalAmount.ShouldBe(119m);
        saved.ExtractedDataJson.ShouldNotBeNull();
    }

    [Test]
    public async Task Process_WhenDocumentUnprocessable_MarksFailedAndReturnsOk()
    {
        await using var context = CreateContext();
        var expense = await SeedDocumentExpenseAsync(context);
        var extraction = new Mock<IStockDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<ExpenseExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnprocessableDocumentException("OpenAI rejected the request: " + new string('x', 5000)));

        var result = await CreateProcessHandler(context, extraction)
            .Handle(new ProcessExpenseDocumentCommand(expense.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var saved = await context.Expenses.SingleAsync();
        saved.Status.ShouldBe(ExpenseStatus.Failed);
        // The provider's error body (here longer than the 2000-char column) must never reach ErrorMessage.
        saved.ErrorMessage.ShouldBe("Documentul nu a putut fi citit. Completează datele manual sau reîncearcă.");
    }

    [Test]
    public async Task Process_WhenTransientFailure_PropagatesAndLeavesProcessing()
    {
        await using var context = CreateContext();
        var expense = await SeedDocumentExpenseAsync(context);
        var extraction = new Mock<IStockDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<ExpenseExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientExtractionException("503"));

        await Should.ThrowAsync<TransientExtractionException>(() => CreateProcessHandler(context, extraction)
            .Handle(new ProcessExpenseDocumentCommand(expense.Id), CancellationToken.None).AsTask());

        (await context.Expenses.SingleAsync()).Status.ShouldBe(ExpenseStatus.Processing);
    }

    [Test]
    public async Task Process_WhenTransientFailureOnFinalAttempt_MarksFailedInsteadOfLeavingProcessing()
    {
        await using var context = CreateContext();
        var expense = await SeedDocumentExpenseAsync(context);
        var extraction = new Mock<IStockDocumentExtractionService>();
        extraction.Setup(e => e.ExtractAsync<ExpenseExtractionResult>(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientExtractionException("503"));

        var result = await CreateProcessHandler(context, extraction, finalAttempt: true)
            .Handle(new ProcessExpenseDocumentCommand(expense.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var saved = await context.Expenses.SingleAsync();
        saved.Status.ShouldBe(ExpenseStatus.Failed);
        saved.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Process_WhenAlreadyHandled_IsIdempotentAndSkipsExtraction()
    {
        await using var context = CreateContext();
        var expense = await SeedDocumentExpenseAsync(context);
        expense.Update("Manual supplier", null, null, "RON", 10m, null, null);
        expense.MarkAsFailed("x");
        expense.Update("Manual supplier", null, null, "RON", 10m, null, null); // user completed it by hand
        await context.SaveChangesAsync();
        var extraction = new Mock<IStockDocumentExtractionService>();

        var result = await CreateProcessHandler(context, extraction)
            .Handle(new ProcessExpenseDocumentCommand(expense.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        extraction.VerifyNoOtherCalls();
        (await context.Expenses.SingleAsync()).SupplierName.ShouldBe("Manual supplier");
    }

    [Test]
    public async Task Process_WhenExpenseMissing_ReturnsFailure()
    {
        await using var context = CreateContext();

        var result = await CreateProcessHandler(context, new Mock<IStockDocumentExtractionService>())
            .Handle(new ProcessExpenseDocumentCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task CreateManual_CreatesProcessedExpense()
    {
        await using var context = CreateContext();
        var (schoolClass, _) = await SeedClassAndFileAsync(context);
        var handler = new CreateExpenseCommandHandler(context, new FakeUser(UserId));

        var result = await handler.Handle(new CreateExpenseCommand
        {
            ClassId = schoolClass.Id,
            SupplierName = "Librarie",
            TotalAmount = 50m,
            Currency = "RON"
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ExpenseStatus.Processed);
        result.Value.Source.ShouldBe(ExpenseSource.Manual);
        (await context.OutboxMessages.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Update_OnFailedExpense_CompletesItAsProcessed()
    {
        await using var context = CreateContext();
        var expense = await SeedDocumentExpenseAsync(context);
        expense.MarkAsFailed("unreadable");
        await context.SaveChangesAsync();

        var result = await new UpdateExpenseCommandHandler(context).Handle(new UpdateExpenseCommand
        {
            Id = expense.Id,
            SupplierName = "Typed by hand",
            Currency = "ron",
            TotalAmount = 20m
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ExpenseStatus.Processed);
        result.Value.ErrorMessage.ShouldBeNull();
        result.Value.Currency.ShouldBe("RON");
    }

    [Test]
    public async Task Update_WhileProcessing_IsRejectedSoTheAiResultIsNotOverwritten()
    {
        await using var context = CreateContext();
        var expense = await SeedDocumentExpenseAsync(context);

        var result = await new UpdateExpenseCommandHandler(context).Handle(
            new UpdateExpenseCommand { Id = expense.Id, Currency = "RON", SupplierName = "x" }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        (await context.Expenses.SingleAsync()).SupplierName.ShouldBeNull();
    }

    [Test]
    public async Task Retry_OnFailedExpense_ReturnsToProcessingAndQueuesAgain()
    {
        await using var context = CreateContext();
        var expense = await SeedDocumentExpenseAsync(context);
        expense.MarkAsFailed("boom");
        await context.SaveChangesAsync();

        var result = await new RetryExpenseProcessingCommandHandler(context, new FakeUser(UserId))
            .Handle(new RetryExpenseProcessingCommand(expense.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var saved = await context.Expenses.SingleAsync();
        saved.Status.ShouldBe(ExpenseStatus.Processing);
        saved.ErrorMessage.ShouldBeNull();
        (await context.OutboxMessages.CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task Retry_WhenNotFailed_ReturnsFailure()
    {
        await using var context = CreateContext();
        var expense = await SeedDocumentExpenseAsync(context);

        var result = await new RetryExpenseProcessingCommandHandler(context, new FakeUser(UserId))
            .Handle(new RetryExpenseProcessingCommand(expense.Id), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        (await context.OutboxMessages.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Delete_SoftDeletesAndHidesFromQueries()
    {
        await using var context = CreateContext();
        var expense = await SeedDocumentExpenseAsync(context);

        var result = await new DeleteExpenseCommandHandler(context)
            .Handle(new DeleteExpenseCommand(expense.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await context.Expenses.CountAsync()).ShouldBe(0);
        (await context.Expenses.IgnoreQueryFilters().CountAsync()).ShouldBe(1);
    }
}
