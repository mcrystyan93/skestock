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
compose.ConfigureComposeFile(file =>
{
    file.Volumes[Services.DataProtectionKeysVolume] =
        new Aspire.Hosting.Docker.Resources.ServiceNodes.Volume
        {
            Name = Services.DataProtectionKeysVolume,
            Driver = "local"
        };
});

// builder.AddAzureContainerAppEnvironment("aca-env");
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sqlServer = builder
    .AddSqlServer(Services.DatabaseServer)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.DatabaseServer;
        // Loopback only: reach it remotely through an SSH tunnel, never directly from the LAN.
        service.Ports.Add("127.0.0.1:1433:1433");
    })
    .WithComputeEnvironment(compose)
    .WithDataVolume(Services.DatabaseVolumes)
    .WithPassword(sqlPassword);
if (builder.ExecutionContext.IsRunMode)
{
    // Published stacks bind SQL Server to the host's loopback only (see PublishAsDockerComposeService).
    sqlServer.WithEndpoint(targetPort: 1433, port: 1433, name: "tcp", isExternal: true);
}

var databaseServer = sqlServer.AddDatabase(Services.Database);

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
var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .PublishAsDockerFile(container =>
        container.WithDockerfile("../..", "src/Web/Dockerfile", "final"))
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.WebApi;
        service.Image = "skestock-webapi:latest";
        service.Build = new Aspire.Hosting.Docker.Resources.ServiceNodes.Build
        {
            Context = "..",
            Dockerfile = "src/Web/Dockerfile",
            Target = "final"
        };
        // aspnet images ship bash but no curl/wget; /health returns 503 when unhealthy.
        service.Healthcheck = new Aspire.Hosting.Docker.Resources.ServiceNodes.Healthcheck
        {
            Test =
            [
                "CMD", "bash", "-c",
                "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /health HTTP/1.0\\r\\nHost: localhost\\r\\n\\r\\n' >&3 && head -n1 <&3 | grep -q ' 200 '"
            ],
            Interval = "30s",
            Timeout = "5s",
            Retries = 3,
            StartPeriod = "60s"
        };
        service.AddVolume(new Aspire.Hosting.Docker.Resources.ServiceNodes.Volume
        {
            Name = Services.DataProtectionKeysVolume,
            Source = Services.DataProtectionKeysVolume,
            Target = Services.DataProtectionKeysPath,
            Type = "volume"
        });
    })
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiKey}", openAiApiKey)
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiModel}", openAiModel)
    .WithComputeEnvironment(compose)
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
    .PublishAsDockerFile(container =>
        container.WithDockerfile("../..", "src/Worker/Dockerfile", "final"))
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.Worker;
        service.Image = "skestock-worker:latest";
        service.Build = new Aspire.Hosting.Docker.Resources.ServiceNodes.Build
        {
            Context = "..",
            Dockerfile = "src/Worker/Dockerfile",
            Target = "final"
        };
        // HeartbeatFileService touches this file every 30s while every queue loop is polling.
        // "$$" is compose's escape for a literal "$".
        service.Healthcheck = new Aspire.Hosting.Docker.Resources.ServiceNodes.Healthcheck
        {
            Test =
            [
                "CMD", "sh", "-c",
                "test $$(( $$(date +%s) - $$(stat -c %Y /tmp/skestock-worker.heartbeat) )) -lt 180"
            ],
            Interval = "60s",
            Timeout = "5s",
            Retries = 3,
            StartPeriod = "120s"
        };
    })
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiKey}", openAiApiKey)
    .WithEnvironment($"{Services.OpenApiSettings}__{Services.OpenApiModel}", openAiModel)
    .WithComputeEnvironment(compose)
    .WaitFor(databaseServer)
    .WithReference(cache)
    .WaitFor(cache);

if (builder.ExecutionContext.IsRunMode)
{
    web.WithReference(databaseServer);
    worker.WithReference(databaseServer);
}

if (builder.ExecutionContext.IsPublishMode)
{
    var sqlAppPassword = builder.AddParameter("sql-app-password", secret: true);
    var sqlMigratorPassword = builder.AddParameter("sql-migrator-password", secret: true);

    // One-shot step: provisions the least-privilege logins as sa, then applies EF migrations as the
    // migrator login. It is the only service (besides dbserver) that receives the sa credentials.
    var migrator = builder.AddProject<Projects.Web>(Services.DatabaseMigrator)
        .PublishAsDockerFile(container =>
            container.WithDockerfile("../..", "src/Web/Dockerfile", "final"))
        .PublishAsDockerComposeService((resource, service) =>
        {
            service.Name = Services.DatabaseMigrator;
            service.Image = "skestock-webapi:latest";
            service.Build = new Aspire.Hosting.Docker.Resources.ServiceNodes.Build
            {
                Context = "..",
                Dockerfile = "src/Web/Dockerfile",
                Target = "final"
            };
            service.Restart = "no";
        })
        .WithComputeEnvironment(compose)
        .WithArgs(Services.MigrateArgument)
        .WithReference(databaseServer)
        .WaitFor(databaseServer)
        .WithEnvironment(
            $"{Services.DatabaseMigrationSettings}__{Services.DatabaseAppPassword}", sqlAppPassword)
        .WithEnvironment(
            $"{Services.DatabaseMigrationSettings}__{Services.DatabaseMigratorPassword}", sqlMigratorPassword);

    var appConnectionString = ReferenceExpression.Create(
        $"Server={Services.DatabaseServer},1433;User ID={Services.DatabaseAppLogin};Password={sqlAppPassword};Encrypt=True;TrustServerCertificate=True;Initial Catalog={Services.Database}");
    foreach (var app in new[] { web, worker })
    {
        app.WithEnvironment($"ConnectionStrings__{Services.Database}", appConnectionString)
            .WaitForCompletion(migrator);
    }

    web.WithEndpoint(targetPort: 8080, port: 7001, name: "http", isExternal: true);
    web
        .WithEnvironment(
            $"{Services.DataProtection}__{Services.DataProtectionKeysDirectory}",
            Services.DataProtectionKeysPath);

    var azurite = builder
        .AddContainer(Services.Storage, "mcr.microsoft.com/azure-storage/azurite:3.35.0")
        .PublishAsDockerComposeService((resource, service) =>
        {
            service.Name = Services.Storage;
        })
        .WithComputeEnvironment(compose)
        .WithArgs(
            "azurite",
            "--location", "/data",
            "--blobHost", "0.0.0.0",
            "--queueHost", "0.0.0.0",
            "--tableHost", "0.0.0.0",
            "--skipApiVersionCheck")
        .WithVolume(Services.StorageVolumes, "/data")
        .WithEndpoint(targetPort: 10000, port: 10000, name: "blob", isExternal: true)
        .WithEndpoint(targetPort: 10001, port: 10001, name: "queue", isExternal: true)
        .WithEndpoint(targetPort: 10002, port: 10002, name: "table", isExternal: true);

    builder
        .AddContainer(Services.CacheCommander, "docker.io/rediscommander/redis-commander:latest")
        .PublishAsDockerComposeService((resource, service) =>
        {
            service.Name = Services.CacheCommander;
        })
        .WithComputeEnvironment(compose)
        .WithEnvironment("REDIS_HOSTS", $"local:{Services.Cache}:6379:0:{redisPassword}")
        .WithEndpoint(targetPort: 8081, port: 8081, name: "http", isExternal: true)
        .WaitFor(cache);

    const string accountName = "devstoreaccount1";
    const string accountKey =
        "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
    var blobConnectionString =
        $"DefaultEndpointsProtocol=http;AccountName={accountName};AccountKey={accountKey};" +
        $"BlobEndpoint=http://{Services.Storage}:10000/{accountName}";
    var queueConnectionString =
        $"DefaultEndpointsProtocol=http;AccountName={accountName};AccountKey={accountKey};" +
        $"QueueEndpoint=http://{Services.Storage}:10001/{accountName}";

    web
        .WithEnvironment($"ConnectionStrings__{Services.BlobService}", blobConnectionString)
        .WithEnvironment($"ConnectionStrings__{Services.Queues}", queueConnectionString)
        .WaitFor(azurite);
    worker
        .WithEnvironment($"ConnectionStrings__{Services.BlobService}", blobConnectionString)
        .WithEnvironment($"ConnectionStrings__{Services.Queues}", queueConnectionString)
        .WaitFor(azurite);

    var publicBlobEndpoint = builder.Configuration["StoragePublicBlobEndpoint"];
    if (string.IsNullOrWhiteSpace(publicBlobEndpoint))
    {
        throw new InvalidOperationException(
            "StoragePublicBlobEndpoint must be configured when publishing.");
    }

    web.WithEnvironment("Storage__PublicBlobEndpoint", publicBlobEndpoint);
    worker.WithEnvironment("Storage__PublicBlobEndpoint", publicBlobEndpoint);
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

builder.Build().Run();
