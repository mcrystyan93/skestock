using skestock.AppHost;
using skestock.Shared;

var builder = DistributedApplication.CreateBuilder(args);

// builder.AddAzureContainerAppEnvironment("aca-env");
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var databaseServer = builder
    .AddSqlServer(Services.DatabaseServer)
    .WithEndpoint(targetPort: 1433, port: 1433, name: "tcp")
    .WithDataVolume(Services.DatabaseVolumes)
    .WithPassword(sqlPassword)
    .AddDatabase(Services.Database);

var redisPassword = builder.AddParameter("redis-password", secret: true);
var cache = builder
    .AddRedis(Services.Cache)
    .WithPassword(redisPassword)
    .WithDataVolume(Services.CacheVolumes)
    .WithPassword(redisPassword)
    .WithEndpoint(targetPort: 6379, port: 6379, name: "tcp");

var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .WithReference(databaseServer)
    .WaitFor(databaseServer)
    .WithReference(cache)
    .WaitFor(cache)
    .WithExternalHttpEndpoints()
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });


builder.Build().Run();
