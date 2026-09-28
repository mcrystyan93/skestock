using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace skestock.Infrastructure.IntegrationTests;

/// <summary>
/// Builds the real <see cref="DependencyInjection.AddInfrastructureServices"/> registration
/// against the TestAppHost resources, so tests exercise production wiring rather than a copy.
/// </summary>
internal static class InfrastructureHost
{
    public static IHost Build(Action<HostApplicationBuilder>? configure = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
            DisableDefaults = true
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Services.Database}"] = IntegrationTestSetup.DatabaseConnectionString,
            [$"ConnectionStrings:{Services.Cache}"] = IntegrationTestSetup.CacheConnectionString,
            [$"ConnectionStrings:{Services.BlobService}"] = IntegrationTestSetup.BlobConnectionString,
            [$"ConnectionStrings:{Services.Queues}"] = IntegrationTestSetup.QueueConnectionString,
            [$"{Services.OpenApiSettings}:{Services.OpenApiKey}"] = "integration-test-key",
            [$"{Services.OpenApiSettings}:{Services.OpenApiModel}"] = "integration-test-model"
        });

        builder.AddInfrastructureServices();
        configure?.Invoke(builder);

        return builder.Build();
    }
}
