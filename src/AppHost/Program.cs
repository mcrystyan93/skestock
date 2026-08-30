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
            .WithTablePort(10002);
    });
var blobs = storage.AddBlobContainer("blobs");
var filesContainer = storage.AddBlobContainer("app-files", blobContainerName: "app-files");

var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = Services.WebApi;
    })
    .WithComputeEnvironment(compose)
    .WithReference(databaseServer)
    .WaitFor(databaseServer)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(blobs)
    .WaitFor(filesContainer)
    .WithExternalHttpEndpoints()
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });

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
