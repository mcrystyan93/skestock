using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Mediator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using Worker.Services;

namespace Worker.UnitTests.Queues;

public enum QueueProcessorKind
{
    Category,
    GoodsReceipt,
    Item
}

internal sealed class QueueProcessingTestHarness : IDisposable
{
    private readonly CancellationTokenSource _stopSource = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly SqliteConnection _connection;

    public QueueProcessingTestHarness(QueueProcessorKind kind, QueueMessage message)
    {
        QueueName = kind switch
        {
            QueueProcessorKind.Category => skestock.Shared.Services.CategoryImportQueue,
            QueueProcessorKind.GoodsReceipt => skestock.Shared.Services.GoodsReceiptImportQueue,
            QueueProcessorKind.Item => skestock.Shared.Services.ItemImportQueue,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
        PoisonQueueName = $"{QueueName}-poison";

        QueueServiceClient = new Mock<QueueServiceClient>(MockBehavior.Strict, "UseDevelopmentStorage=true");
        MainQueueClient = new Mock<QueueClient>(MockBehavior.Strict, "UseDevelopmentStorage=true", QueueName);
        PoisonQueueClient = new Mock<QueueClient>(MockBehavior.Strict, "UseDevelopmentStorage=true", PoisonQueueName);
        Sender = new Mock<ISender>(MockBehavior.Strict);

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<WorkerTestDbContext>()
            .UseSqlite(_connection)
            .Options;
        DbContext = new WorkerTestDbContext(dbOptions);
        DbContext.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(DbContext);
        services.AddScoped<ISender>(_ => Sender.Object);
        services.AddScoped<AmbientUser>(_ =>
        {
            var ambientUser = new AmbientUser();
            AmbientUsers.Add(ambientUser);
            return ambientUser;
        });
        _serviceProvider = services.BuildServiceProvider();

        QueueServiceClient
            .Setup(client => client.GetQueueClient(QueueName))
            .Returns(MainQueueClient.Object);
        QueueServiceClient
            .Setup(client => client.GetQueueClient(PoisonQueueName))
            .Returns(PoisonQueueClient.Object);

        SetupQueueOperations(MainQueueClient);
        SetupQueueOperations(PoisonQueueClient);
        SetupMessagePolling(message);
        SourceMessageText = message.MessageText;
    }

    public string QueueName { get; }
    public string PoisonQueueName { get; }
    public string SourceMessageText { get; }
    public Mock<QueueServiceClient> QueueServiceClient { get; }
    public Mock<QueueClient> MainQueueClient { get; }
    public Mock<QueueClient> PoisonQueueClient { get; }
    public Mock<ISender> Sender { get; }
    public WorkerTestDbContext DbContext { get; }
    public IServiceScopeFactory ScopeFactory => _serviceProvider.GetRequiredService<IServiceScopeFactory>();
    public CancellationToken StopToken => _stopSource.Token;
    public List<object> SentRequests { get; } = [];
    public List<Guid?> ObservedUserIds { get; } = [];
    public List<string> PoisonMessages { get; } = [];
    public List<(int? MaxMessages, TimeSpan? VisibilityTimeout)> ReceiveCalls { get; } = [];
    public int DeleteCount { get; private set; }

    public void ConfigureSender(Func<object, CancellationToken, ValueTask<object?>> handler)
    {
        Sender
            .Setup(sender => sender.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns((object request, CancellationToken cancellationToken) =>
            {
                SentRequests.Add(request);
                ObservedUserIds.Add(AmbientUsers[^1].Id);
                return handler(request, cancellationToken);
            });
    }

    public void AddProcessedMessage(Guid messageId)
    {
        DbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            Id = messageId,
            ProcessedAtUtc = DateTime.UtcNow
        });
        DbContext.SaveChanges();
    }

    public void Stop() => _stopSource.Cancel();

    public void Dispose()
    {
        _stopSource.Dispose();
        _serviceProvider.Dispose();
        DbContext.Dispose();
        _connection.Dispose();
    }

    private List<AmbientUser> AmbientUsers { get; } = [];

    private void SetupQueueOperations(Mock<QueueClient> queueClient)
    {
        queueClient
            .Setup(client => client.CreateIfNotExistsAsync(
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());
    }

    private void SetupMessagePolling(QueueMessage message)
    {
        MainQueueClient
            .Setup(client => client.ReceiveMessagesAsync(
                It.IsAny<int?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((int? maxMessages, TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
            {
                ReceiveCalls.Add((maxMessages, visibilityTimeout));
                return Task.FromResult<Response<QueueMessage[]>>(
                    Response.FromValue<QueueMessage[]>([message], Mock.Of<Response>()));
            });

        MainQueueClient
            .Setup(client => client.DeleteMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                DeleteCount++;
                _stopSource.Cancel();
            })
            .ReturnsAsync(Mock.Of<Response>());

        PoisonQueueClient
            .Setup(client => client.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback((string messageText, CancellationToken _) =>
                PoisonMessages.Add(messageText))
            .ReturnsAsync(Response.FromValue<SendReceipt>(
                default(SendReceipt)!,
                Mock.Of<Response>()));
    }
}

internal static class QueueMessageFactory
{
    public static QueueMessage Create(
        QueueProcessorKind kind,
        Guid messageId,
        Guid? userId = null,
        int dequeueCount = 1)
    {
        object command = kind switch
        {
            QueueProcessorKind.Category => new
            {
                CategoryImportBatchId = Guid.NewGuid()
            },
            QueueProcessorKind.GoodsReceipt => new
            {
                GoodsReceiptImportId = Guid.NewGuid()
            },
            QueueProcessorKind.Item => new
            {
                ItemImportBatchId = Guid.NewGuid()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        var type = kind switch
        {
            QueueProcessorKind.Category =>
                typeof(skestock.Application.Features.Categories.Commands.ProcessCategoryImportBatch.ProcessCategoryImportBatchCommand),
            QueueProcessorKind.GoodsReceipt =>
                typeof(skestock.Application.Features.GoodsReceipts.Commands.ProcessGoodsReceiptImport.ProcessGoodsReceiptImportCommand),
            QueueProcessorKind.Item =>
                typeof(skestock.Application.Features.Items.Commands.ProcessItemImportBatch.ProcessItemImportBatchCommand),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        var envelope = new MessageEnvelope
        {
            MessageId = messageId,
            Type = type.AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(command),
            UserId = userId
        };

        return CreateRaw(envelope, dequeueCount);
    }

    public static QueueMessage CreateRaw(MessageEnvelope envelope, int dequeueCount = 1)
    {
        var json = JsonSerializer.Serialize(envelope);
        var messageText = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return QueuesModelFactory.QueueMessage(
            messageId: Guid.NewGuid().ToString(),
            popReceipt: "pop-receipt",
            messageText: messageText,
            dequeueCount: dequeueCount);
    }

    public static QueueMessage CreateRawText(string messageText, int dequeueCount = 1) =>
        QueuesModelFactory.QueueMessage(
            messageId: Guid.NewGuid().ToString(),
            popReceipt: "pop-receipt",
            messageText: messageText,
            dequeueCount: dequeueCount);
}

internal sealed class WorkerTestDbContext(DbContextOptions<WorkerTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImportBatch> CategoryImportBatches => Set<CategoryImportBatch>();
    public DbSet<CategoryImportBatchFile> CategoryImportBatchFiles => Set<CategoryImportBatchFile>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemImportBatch> ItemImportBatches => Set<ItemImportBatch>();
    public DbSet<ItemImportBatchFile> ItemImportBatchFiles => Set<ItemImportBatchFile>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<OrderList> OrderLists => Set<OrderList>();
    public DbSet<OrderListLine> OrderListLines => Set<OrderListLine>();
    public DbSet<WorkerTestEffect> TestEffects => Set<WorkerTestEffect>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProcessedMessage>().HasKey(message => message.Id);
        modelBuilder.Entity<WorkerTestEffect>().HasKey(effect => effect.Id);
        modelBuilder.Ignore<Category>();
        modelBuilder.Ignore<CategoryImportBatch>();
        modelBuilder.Ignore<CategoryImportBatchFile>();
        modelBuilder.Ignore<ClassBalance>();
        modelBuilder.Ignore<FileMetadata>();
        modelBuilder.Ignore<Item>();
        modelBuilder.Ignore<ItemImportBatch>();
        modelBuilder.Ignore<ItemImportBatchFile>();
        modelBuilder.Ignore<Location>();
        modelBuilder.Ignore<SchoolClass>();
        modelBuilder.Ignore<StockBatch>();
        modelBuilder.Ignore<StockTransaction>();
        modelBuilder.Ignore<GoodsReceipt>();
        modelBuilder.Ignore<GoodsReceiptImport>();
        modelBuilder.Ignore<GoodsReceiptImportLine>();
        modelBuilder.Ignore<UserProfile>();
        modelBuilder.Ignore<OutboxMessage>();
        modelBuilder.Ignore<OrderList>();
        modelBuilder.Ignore<OrderListLine>();
    }
}

internal sealed class WorkerTestEffect
{
    public Guid Id { get; set; }
}
