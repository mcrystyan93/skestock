using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using skestock.Application.Common.Interfaces;
using skestock.Web.BackgroundJobs;
using skestock.Web.Services;
using skestock.Infrastructure.Storage;
using StackExchange.Redis;

namespace skestock.Web;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Antiforgery: JS-readable "XSRF-TOKEN" cookie + "X-XSRF-TOKEN" header match Angular's
        // built-in withXsrfConfiguration() defaults, so no custom header name is needed client-side.
        // builder.Services.AddAntiforgery(options =>
        // {
        //     options.HeaderName = "X-XSRF-TOKEN";
        //     options.Cookie.Name = "XSRF-TOKEN";
        //     options.Cookie.HttpOnly = false;
        //     // options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        //     //     ? CookieSecurePolicy.None
        //     //     : CookieSecurePolicy.Always;
        //     
        //     options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        //     options.Cookie.SameSite = SameSiteMode.Strict;
        // });

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        // Fallback ProblemDetails generation for exceptions not handled by ProblemDetailsExceptionHandler,
        // so any unhandled exception still returns an RFC 9110-compliant ProblemDetails response.
        // Identity API login failures are returned as ProblemHttpResult values rather than thrown
        // exceptions, so customize those results here instead of relying on the exception handler.
        builder.Services.AddProblemDetails(options =>
        {
            var invalidCredentialsCode = "auth.invalid_credentials";

            options.CustomizeProblemDetails = context =>
            {
                var isLoginRequest =
                    context.HttpContext.Request.Path.Value?.EndsWith(
                        "/login",
                        StringComparison.OrdinalIgnoreCase) == true;

                if (!isLoginRequest ||
                    context.ProblemDetails.Status != StatusCodes.Status401Unauthorized)
                {
                    return;
                }

                context.ProblemDetails.Type =
                    "https://tools.ietf.org/html/rfc9110#section-15.5.2";
                context.ProblemDetails.Title = "Authentication failed";
                context.ProblemDetails.Detail = "Invalid email or password.";
                context.ProblemDetails.Extensions[ApiErrorExtensions.Error] =
                    ApiErrorContractFactory.Create(
                        code: invalidCredentialsCode,
                        httpContext: context.HttpContext);
            };
        });

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApi(options =>
        {
            options.AddOperationTransformer<ApiExceptionOperationTransformer>();
            options.AddOperationTransformer<IdentityApiOperationTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        builder.Services.AddCors();

        builder.Services.AddHostedService<OutboxPublisherService>();
        builder.Services.AddHostedService<AzureBlobCorsInitializer>();

    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    }
}
