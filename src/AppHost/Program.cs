using Aspire.Hosting.Azure;
using Azure.Storage.Blobs.Models;
using skestock.AppHost;
using skestock.Shared;

var builder = DistributedApplication.CreateBuilder(args);

var publicOrigins = (builder.Configuration["PublicOrigins"]
                     ?? (builder.ExecutionContext.IsRunMode
                         ? "http://webfrontend-skestock.dev.localhost:7001,http://127.0.0.1:7001"
                         : throw new InvalidOperationException(
                             "PublicOrigins must be configured when publishing.")))
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

if (publicOrigins.Length == 0)
{
    throw new InvalidOperationException("At least one public origin must be configured.");
}

var compose = builder.AddDockerComposeEnvironment("env")
    .WithDashboard(dashboard =>
    {
        dashboard.WithHostPort(18080)
            .WithForwardedHeaders(enabled: true);
    });

// builder.AddAzureContainerAppEnvironment("aca-env");
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var databaseServer = builder
    .AddSqlServer(Services.DatabaseServer)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.DatabaseServer;
    })
    .WithComputeEnvironment(compose)
    .WithEndpoint(targetPort: 1433, port: 1433, name: "tcp", isExternal: true)
    .WithDataVolume(Services.DatabaseVolumes)
    .WithPassword(sqlPassword)
    .AddDatabase(Services.Database);

var redisPassword = builder.AddParameter("redis-password", secret: true);
var cache = builder
    .AddRedis(Services.Cache)
    .WithRedisCommander(commander =>
    {
        commander.WithComputeEnvironment(compose)
            .WithEndpoint(targetPort: 8081, port: 8081, name: "http", isExternal: true);
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.Cache;
    })
    .WithComputeEnvironment(compose)
    .WithPassword(redisPassword)
    .WithDataVolume(Services.CacheVolumes)
    .WithEndpoint(targetPort: 6379, port: 6379, name: "tcp", isExternal: true);

var openAiApiKey = builder.AddParameter($"{Services.OpenApiSettings}{Services.OpenApiKey}", secret:true);
var openAiModel = builder.AddParameter($"{Services.OpenApiSettings}{Services.OpenApiModel}", "gpt-5.6-luna");
// change
var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.WebApi;
    })
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiKey}", openAiApiKey)
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiModel}", openAiModel)
    .WithComputeEnvironment(compose)
    .WithReference(databaseServer)
    .WaitFor(databaseServer)
    .WithReference(cache)
    .WaitFor(cache)
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });

var worker = builder.AddProject<Projects.Worker>(Services.Worker)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.Worker;
    })
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiKey}", openAiApiKey)
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiModel}", openAiModel)
    .WithComputeEnvironment(compose)
    .WithReference(databaseServer)
    .WaitFor(databaseServer)
    .WithReference(cache)
    .WaitFor(cache);

if (builder.ExecutionContext.IsPublishMode)
{
    web.WithEndpoint(targetPort: 8080, port: 7001, name: "http", isExternal: true);

    var azurite = builder
        .AddContainer(Services.Storage, "mcr.microsoft.com/azure-storage/azurite:3.35.0")
        .PublishAsDockerComposeService((resource, service) =>
        {
            service.Name = Services.Storage;
        })
        .WithComputeEnvironment(compose)
        .WithArgs(
            "--location", "/data",
            "--blobHost", "0.0.0.0",
            "--queueHost", "0.0.0.0",
            "--tableHost", "0.0.0.0")
        .WithVolume(Services.StorageVolumes, "/data")
        .WithEndpoint(targetPort: 10000, port: 10000, name: "blob", isExternal: true)
        .WithEndpoint(targetPort: 10001, port: 10001, name: "queue", isExternal: true)
        .WithEndpoint(targetPort: 10002, port: 10002, name: "table", isExternal: true);

    builder
        .AddContainer(Services.CacheCommander, "rediscommander/redis-commander:latest")
        .PublishAsDockerComposeService((resource, service) =>
        {
            service.Name = Services.CacheCommander;
        })
        .WithComputeEnvironment(compose)
        .WithEnvironment("REDIS_HOSTS", $"local:{Services.Cache}:6379")
        .WithEnvironment("REDIS_PASSWORD", redisPassword)
        .WithEndpoint(targetPort: 8081, port: 8081, name: "http", isExternal: true)
        .WaitFor(cache);

    var storageHost = builder.Configuration["StoragePublicHost"]
        ?? new Uri(publicOrigins[0]).Host;
    if (storageHost is "localhost" or "127.0.0.1" or "::1")
    {
        throw new InvalidOperationException(
            "StoragePublicHost or PublicOrigins must use a hostname reachable from the deployed containers and browser.");
    }

    const string accountName = "devstoreaccount1";
    const string accountKey =
        "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1Oc8g4w8r3F2mZ8T9h5e0r3p5m7m8r9s0u1v2w3x4y5z6A7B8C9D0E1F2G3H4I5J6K7L8M9N0";
    var blobConnectionString =
        $"DefaultEndpointsProtocol=http;AccountName={accountName};AccountKey={accountKey};" +
        $"BlobEndpoint=http://{storageHost}:10000/{accountName}";
    var queueConnectionString =
        $"DefaultEndpointsProtocol=http;AccountName={accountName};AccountKey={accountKey};" +
        $"QueueEndpoint=http://{storageHost}:10001/{accountName}";

    web
        .WithEnvironment($"ConnectionStrings__{Services.BlobService}", blobConnectionString)
        .WithEnvironment($"ConnectionStrings__{Services.Queues}", queueConnectionString)
        .WaitFor(azurite);
    worker
        .WithEnvironment($"ConnectionStrings__{Services.BlobService}", blobConnectionString)
        .WithEnvironment($"ConnectionStrings__{Services.Queues}", queueConnectionString)
        .WaitFor(azurite);
}
else
{
    var storage = builder
        .AddAzureStorage(Services.Storage)
        .RunAsEmulator(azurite =>
        {
            azurite.WithLifetime(ContainerLifetime.Persistent)
                .WithDataVolume(Services.StorageVolumes)
                .WithBlobPort(10000)
                .WithQueuePort(10001)
                .WithTablePort(10002)
                .WithComputeEnvironment(compose);
        });
    var filesContainer = storage.AddBlobContainer("app-files", blobContainerName: "app-files");
    var blobService = storage.AddBlobs(Services.BlobService);
    var queue = storage.AddQueues(Services.Queues);

    storage.SetBlobCorsRules(new[]
    {
        new BlobCorsRule
        {
            AllowedOrigins = string.Join(",", publicOrigins),
            AllowedMethods = "GET,POST,PUT,DELETE,OPTIONS",
            AllowedHeaders = "*",
            ExposedHeaders = "*",
            MaxAgeInSeconds = 3600
        }
    });

    web
        .WithReference(blobService)
        .WaitFor(filesContainer)
        .WithReference(queue)
        .WaitFor(queue);
    worker
        .WithReference(blobService)
        .WaitFor(filesContainer)
        .WithReference(queue)
        .WaitFor(queue);
}

var webfrontend = builder.AddViteApp(Services.WebFrontend, "../Client", "dev")
    .WithComputeEnvironment(compose)
    .WithReference(web)
    .WaitFor(web)
    .WithEnvironment("ASPNETCORE_URLS", web.GetEndpoint("http"))
    .WithNpm()
    .WithHttpEndpoint(port: 7001, env: "PORT")
    .WithExternalHttpEndpoints();

for (var index = 0; index < publicOrigins.Length; index++)
{
    web.WithEnvironment($"Cors__AllowedOrigins__{index}", publicOrigins[index]);
}

// In publish mode the Angular build is copied into Web/wwwroot. Web already serves that directory,
// so the frontend is build-only for deployment while remaining a Vite dev server in run mode.
web.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
