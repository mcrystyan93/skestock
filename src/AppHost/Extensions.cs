using Aspire.Hosting.Azure;
using Azure;
using Azure.Provisioning;
using Azure.Provisioning.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace skestock.AppHost;

internal static class AspireExtensions
{
    public static IResourceBuilder<T> WithAspNetCoreEnvironment<T>(this IResourceBuilder<T> builder) 
        where T : IResourceWithEnvironment
    {
        builder.WithEnvironment(context =>
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] =
                builder.ApplicationBuilder.ExecutionContext.IsPublishMode
                    ? "Production"
                    : environment ?? "Development";
        });

        return builder;
    }
    
     /// <summary>
    /// Sets the CORS rules for the blob service of the storage resource.
    /// Applies to the Azure resource during deployment and also during run mode (e.g., when using the emulator).
    /// </summary>
    public static IResourceBuilder<AzureStorageResource> SetBlobCorsRules(
        this IResourceBuilder<AzureStorageResource> storage,
        IEnumerable<BlobCorsRule> rules
    )
    {
        storage.ConfigureInfrastructure(storageAccount =>
        {
            var blobService = storageAccount.GetProvisionableResources().OfType<BlobService>().Single();
            blobService.CorsRules =
            [
                .. rules.Select(rule => new BicepValue<StorageCorsRule>(
                    new StorageCorsRule()
                    {
                        AllowedHeaders =
                        [
                            .. rule
                                .AllowedHeaders.Split(
                                    ',',
                                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                                )
                                .Select(h => new BicepValue<string>(h)),
                        ],
                        AllowedMethods =
                        [
                            .. rule
                                .AllowedMethods.Split(
                                    ',',
                                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                                )
                                .Select(m =>
                                    m switch
                                    {
                                        "GET" => CorsRuleAllowedMethod.Get,
                                        "POST" => CorsRuleAllowedMethod.Post,
                                        "PUT" => CorsRuleAllowedMethod.Put,
                                        "DELETE" => CorsRuleAllowedMethod.Delete,
                                        "HEAD" => CorsRuleAllowedMethod.Head,
                                        "OPTIONS" => CorsRuleAllowedMethod.Options,
                                        _ => throw new InvalidOperationException($"Invalid CORS method: {m}"),
                                    }
                                ),
                        ],
                        AllowedOrigins =
                        [
                            .. rule
                                .AllowedOrigins.Split(
                                    ',',
                                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                                )
                                .Select(o => new BicepValue<string>(o)),
                        ],
                        ExposedHeaders =
                        [
                            .. rule
                                .ExposedHeaders.Split(
                                    ',',
                                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                                )
                                .Select(e => new BicepValue<string>(e)),
                        ],
                        MaxAgeInSeconds = rule.MaxAgeInSeconds,
                    }
                )),
            ];
        });

        return storage;
    }
}
