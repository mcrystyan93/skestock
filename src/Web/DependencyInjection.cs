using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using skestock.Application.Common.Interfaces;
using skestock.Web.Services;

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
        builder.Services.AddProblemDetails();
        
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
