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
            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = environment ?? "Development";
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

        if (storage.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            var appBuilder = storage.ApplicationBuilder;
            var logger = appBuilder
                .Services.BuildServiceProvider()
                .GetRequiredService<ILogger<DistributedApplication>>();

            // `First`, not `Single` because `AddBlobContainer` and `AddBlobs` both create one
            // Assume it doesn't matter which one we use, because they are both the same blob service
            var blobResource = appBuilder
                .Resources.OfType<AzureBlobStorageResource>()
                .First(resource => resource.Parent == storage.Resource);

            storage.OnResourceReady(
                async (_, _, cancellationToken) =>
                {
                    var connectionString = await blobResource
                        .ConnectionStringExpression.GetValueAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        throw new InvalidOperationException("Blob connection string is null or empty.");
                    }

                    var client = new BlobServiceClient(connectionString);

                    const int maxAttempts = 10;
                    for (var attempt = 1; attempt <= maxAttempts; attempt++)
                    {
                        try
                        {
                            var propertiesResult = await client
                                .GetPropertiesAsync(cancellationToken: cancellationToken)
                                .ConfigureAwait(false);

                            var properties = propertiesResult.Value;

                            properties.Cors = [.. rules];

                            await client
                                .SetPropertiesAsync(properties, cancellationToken: cancellationToken)
                                .ConfigureAwait(false);

                            logger.LogInformation(
                                "Applied blob CORS rule(s) to storage resource '{StorageResourceName}'.",
                                storage.Resource.Name
                            );
                            return;
                        }
                        catch (Exception ex)
                            when (attempt < maxAttempts
                                && (ex is RequestFailedException or HttpRequestException or IOException)
                            )
                        {
                            logger.LogDebug(
                                ex,
                                "Failed to apply blob CORS rule(s) on attempt {Attempt}/{MaxAttempts} for '{StorageResourceName}'. Retrying.",
                                attempt,
                                maxAttempts,
                                storage.Resource.Name
                            );

                            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                        }
                    }

                    throw new InvalidOperationException(
                        $"Failed to apply blob CORS rule(s) to storage resource '{storage.Resource.Name}' after {maxAttempts} attempts."
                    );
                }
            );
        }

        return storage;
    }
}
