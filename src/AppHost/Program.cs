using Aspire.Hosting.Azure;
using Azure.Storage.Blobs.Models;
using skestock.AppHost;
using skestock.Shared;

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("env")
    .WithDashboard(dashboard =>
    {
        dashboard.WithHostPort(8080)
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
    .WithEndpoint(targetPort: 1433, port: 1433, name: "tcp")
    .WithDataVolume(Services.DatabaseVolumes)
    .WithPassword(sqlPassword)
    .AddDatabase(Services.Database);

var redisPassword = builder.AddParameter("redis-password", secret: true);
var cache = builder
    .AddRedis(Services.Cache)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.Cache;
    })
    .WithComputeEnvironment(compose)
    .WithPassword(redisPassword)
    .WithDataVolume(Services.CacheVolumes)
    .WithPassword(redisPassword)
    .WithEndpoint(targetPort: 6379, port: 6379, name: "tcp");

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

storage.SetBlobCorsRules(new[]
{
    new BlobCorsRule
    {
        AllowedOrigins = "http://webfrontend-skestock.dev.localhost:7001,http://127.0.0.1:7001",
        AllowedMethods = "GET,POST,PUT,DELETE,OPTIONS",
        AllowedHeaders = "*",
        ExposedHeaders = "*",
        MaxAgeInSeconds = 3600
    }
});

var queue = storage.AddQueues(Services.Queues);

var documentExtractionProvider = builder.AddParameter(Services.DocumentExtractionProvider, "OpenAI");

var nutrientApiKey = builder.AddParameter($"{Services.NutrientApiSettings}{Services.NutrientApiKey}", secret: true);
var nutrientBaseUrl =
    builder.AddParameter($"{Services.NutrientApiSettings}{Services.NutrientBaseUrl}", "https://api.nutrient.io");

var geminiApiKey = builder.AddParameter($"{Services.GeminiApiSettings}{Services.GeminiApiKey}", secret: true);
var geminiModel = builder.AddParameter($"{Services.GeminiApiSettings}{Services.GeminiModel}", "gemini-3.7-flash");

var openAiApiKey = builder.AddParameter($"{Services.OpenApiSettings}{Services.OpenApiKey}", secret:true);
var openAiModel = builder.AddParameter($"{Services.OpenApiSettings}{Services.OpenApiModel}", "gpt-5.6-luna");
// change
var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.WebApi;
    })
    .WithEnvironment($"{Services.NutrientApiSettings}__{Services.NutrientApiKey}", nutrientApiKey)
    .WithEnvironment($"{Services.NutrientApiSettings}__{Services.NutrientBaseUrl}", nutrientBaseUrl)
    .WithEnvironment($"{Services.DocumentExtractionSettings}__{Services.DocumentExtractionProvider}",
        documentExtractionProvider)
    .WithEnvironment($"{Services.GeminiApiSettings}__{Services.GeminiApiKey}", geminiApiKey)
    .WithEnvironment($"{Services.GeminiApiSettings}__{Services.GeminiModel}", geminiModel)
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiKey}", openAiApiKey)
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiModel}", openAiModel)
    .WithComputeEnvironment(compose)
    .WithReference(databaseServer)
    .WaitFor(databaseServer)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(blobService)
    .WaitFor(filesContainer)
    .WithReference(queue)
    .WaitFor(queue)
    .WithExternalHttpEndpoints()
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
    .WithEnvironment($"{Services.NutrientApiSettings}__{Services.NutrientApiKey}", nutrientApiKey)
    .WithEnvironment($"{Services.NutrientApiSettings}__{Services.NutrientBaseUrl}", nutrientBaseUrl)
    .WithEnvironment($"{Services.DocumentExtractionSettings}__{Services.DocumentExtractionProvider}",
        documentExtractionProvider)
    .WithEnvironment($"{Services.GeminiApiSettings}__{Services.GeminiApiKey}", geminiApiKey)
    .WithEnvironment($"{Services.GeminiApiSettings}__{Services.GeminiModel}", geminiModel)
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiKey}", openAiApiKey)
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiModel}", openAiModel)
    .WithComputeEnvironment(compose)
    .WithReference(databaseServer)
    .WaitFor(databaseServer)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(blobService)
    .WaitFor(filesContainer)
    .WithReference(queue)
    .WaitFor(queue);

var webfrontend = builder.AddViteApp(Services.WebFrontend, "../Client", "dev")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.WebFrontend;
    })
    .WithComputeEnvironment(compose)
    .WithReference(web)
    .WaitFor(web)
    .WithEnvironment("ASPNETCORE_URLS", web.GetEndpoint("http"))
    .WithNpm()
    .WithHttpEndpoint(port: 7001, env: "PORT")
    .WithExternalHttpEndpoints();

// Feed the frontend's Aspire-assigned origin into Web's CORS policy (AllowCredentials() requires
// an explicit origin allowlist, not AllowAnyOrigin()) so cookie-authenticated requests work.
web.WithEnvironment("Cors__AllowedOrigins__0", webfrontend.GetEndpoint("http"));

builder.Build().Run();
