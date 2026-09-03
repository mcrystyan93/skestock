using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models.Options;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Documents.Schemas;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Application.Queues.Interfaces;
using skestock.Application.Storage.Interfaces;
using skestock.Infrastructure.AI;
using skestock.Infrastructure.AI.Schemas;
using skestock.Infrastructure.Data;
using skestock.Infrastructure.Data.Interceptors;
using skestock.Infrastructure.Identity;
using skestock.Infrastructure.Queues;
using skestock.Infrastructure.Storage;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace skestock.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(Services.Database);
        Guard.Against.Null(connectionString, message: $"Connection string '{Services.Database}' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.EnrichSqlServerDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        // Core authorization services (IAuthorizationService / policy provider) with no
        // endpoint-routing dependency, so this method is safe to call from a non-web host
        // (e.g. the Worker). The endpoint-bound web auth wiring lives in
        // AddWebAuthenticationServices below and is called only by the Web host.
        builder.Services.AddAuthorizationCore();

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();

        builder.AddRedisDistributedCache(Services.Cache);
        builder.Services.AddHybridCache();

        builder.AddAzureBlobServiceClient(Services.BlobService);
        builder.AddAzureQueueServiceClient(Services.Queues);

        builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        builder.Services.AddScoped<IQueueSender, AzureQueueSender>();

        builder.Services
            .AddScoped<IExtractionSchemaFactory<GoodsReceiptExtractionResult>,
                GoodsReceiptExtractionSchemaFactory>();

        builder.AddDocumentExtractionServices();
    }

    private static void AddDocumentExtractionServices(this IHostApplicationBuilder builder)
    {
        var provider = builder.Configuration.GetValue<ExtractionProvider>(
            $"{Services.DocumentExtractionSettings}:{Services.DocumentExtractionProvider}");

        switch (provider)
        {
            case ExtractionProvider.Gemini:
                AddGeminiExtraction(builder);
                break;
            case ExtractionProvider.OpenAI:
                AddOpenAiExtraction(builder);
                break;
            case ExtractionProvider.Nutrient:
            default:
                AddNutrientExtraction(builder);
                break;
        }
    }

    private static void AddGeminiExtraction(IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<GeminiApiSettings>()
            .BindConfiguration(Services.GeminiApiSettings)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddScoped<IDocumentExtractionService, GeminiDocumentExtractionService>();
    }

    private static void AddNutrientExtraction(IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<NutrientApiSettings>()
            .BindConfiguration(Services.NutrientApiSettings)
            .ValidateDataAnnotations()
            .ValidateOnStart();

#pragma warning disable EXTEXP0001
        builder.Services.AddHttpClient<IDocumentExtractionService, NutrientDocumentExtractionService>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration.GetValue<string>($"{Services.NutrientApiSettings}:{Services.NutrientBaseUrl}") ??
                                         throw new InvalidOperationException());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                builder.Configuration.GetValue<string>($"{Services.NutrientApiSettings}:{Services.NutrientApiKey}") ??
                throw new InvalidOperationException());
        })
        .RemoveAllResilienceHandlers()
#pragma warning restore EXTEXP0001
            .AddStandardResilienceHandler(options =>
        {
            // per-attempt timeout — must exceed how long a single extraction call can take
            options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);

            // overall timeout across all retries — must be >= AttemptTimeout, higher if you allow retries
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);

            // be conservative on retries for a slow, potentially expensive call
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.Delay = TimeSpan.FromSeconds(2);

            // circuit breaker sampling duration must be >= 2x AttemptTimeout — the library enforces this
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(4);
        });
    }

    private static void AddOpenAiExtraction(IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<OpenAiApiSettings>()
            .BindConfiguration(Services.OpenApiSettings)
            .ValidateDataAnnotations()
            .ValidateOnStart();

#pragma warning disable EXTEXP0001
        builder.Services.AddHttpClient<IDocumentExtractionService, OpenAiDocumentExtractionService>(client =>
            {
                client.BaseAddress = new Uri("https://api.openai.com/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                    builder.Configuration.GetValue<string>($"{Services.OpenApiSettings}:{Services.OpenApiKey}") ??
                    throw new InvalidOperationException());
            })
            .RemoveAllResilienceHandlers()
#pragma warning restore EXTEXP0001
            .AddStandardResilienceHandler(options =>
            {
                // per-attempt timeout — must exceed how long a single extraction call can take
                options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);

                // overall timeout across all retries — must be >= AttemptTimeout, higher if you allow retries
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);

                // be conservative on retries for a slow, potentially expensive call
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.Delay = TimeSpan.FromSeconds(2);

                // circuit breaker sampling duration must be >= 2x AttemptTimeout — the library enforces this
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(4);
            });
    }

    /// <summary>
    /// Registers the endpoint/browser-bound authentication wiring required by the Web host only:
    /// the cookie/bearer authentication schemes, the full (endpoint-aware) authorization services,
    /// and the ASP.NET Core Identity API endpoints consumed by <c>MapIdentityApi&lt;ApplicationUser&gt;()</c>.
    /// It is intentionally separate from <see cref="AddInfrastructureServices"/> because
    /// <c>AddAuthorizationBuilder()</c> pulls in <c>AuthorizationPolicyCache</c>, which depends on
    /// <c>EndpointDataSource</c> and therefore cannot be resolved in a non-web host (e.g. the Worker).
    /// </summary>
    public static void AddWebAuthenticationServices(this IHostApplicationBuilder builder)
    {
        // The web client uses the Identity application cookie (`useCookies=true`),
        // and authorization needs a default scheme when it challenges an anonymous
        // request (for example, POST /api/Users/logout).
        builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddBearerToken(IdentityConstants.BearerScheme)
            .AddCookie(IdentityConstants.ApplicationScheme);

        builder.Services.AddAuthorizationBuilder();

        // The identity core services (UserManager/roles/EF stores) are registered in
        // AddInfrastructureServices; here we only add the identity API endpoints on top.
        new IdentityBuilder(typeof(ApplicationUser), builder.Services)
            .AddApiEndpoints();
    }
}
