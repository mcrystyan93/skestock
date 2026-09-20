using Aspire.Hosting.Azure;
using skestock.Shared;

namespace skestock.TestAppHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var database = builder.AddSqlServer(Services.DatabaseServer)
            .AddDatabase(Services.Database);

        var cache = builder.AddRedis(Services.Cache);

        var storage = builder
            .AddAzureStorage(Services.Storage)
            .RunAsEmulator();
        var filesContainer = storage.AddBlobContainer("app-files", blobContainerName: "app-files");
        var blobService = storage.AddBlobs(Services.BlobService);
        var queues = storage.AddQueues(Services.Queues);

        builder.AddProject<Projects.Worker>(Services.Worker)
            .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiKey}", "functional-test-api-key")
            .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiModel}", "functional-test-model")
            .WithReference(database)
            .WaitFor(database)
            .WithReference(cache)
            .WaitFor(cache)
            .WithReference(blobService)
            .WaitFor(filesContainer)
            .WithReference(queues)
            .WaitFor(queues);

        builder.Build().Run();
    }
}