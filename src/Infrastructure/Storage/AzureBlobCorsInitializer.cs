using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace skestock.Infrastructure.Storage;

public sealed class AzureBlobCorsInitializer(
    BlobServiceClient blobServiceClient,
    IConfiguration configuration,
    ILogger<AzureBlobCorsInitializer> logger) : IHostedService
{
    private const string FilesContainerName = "app-files";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var origins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .ToArray() ?? [];

        if (origins.Length == 0)
        {
            logger.LogWarning("No blob CORS origins are configured; skipping blob CORS initialization.");
            return;
        }

        var rules = new[]
        {
            new BlobCorsRule
            {
                AllowedOrigins = string.Join(",", origins),
                AllowedMethods = "GET,POST,PUT,DELETE,OPTIONS",
                AllowedHeaders = "*",
                ExposedHeaders = "*",
                MaxAgeInSeconds = 3600
            }
        };

        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var properties = (await blobServiceClient
                        .GetPropertiesAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false))
                    .Value;

                await blobServiceClient
                    .GetBlobContainerClient(FilesContainerName)
                    .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                properties.Cors = rules;
                await blobServiceClient
                    .SetPropertiesAsync(properties, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                logger.LogInformation("Applied blob CORS rules for {OriginCount} configured origin(s).", origins.Length);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts &&
                                       (ex is RequestFailedException or HttpRequestException or IOException))
            {
                logger.LogDebug(
                    ex,
                    "Blob CORS initialization attempt {Attempt}/{MaxAttempts} failed; retrying.",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException(
            $"Failed to initialize blob CORS after {maxAttempts} attempts.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
