using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace skestock.Web.Infrastructure;

/// <summary>
/// Validates the antiforgery token (via the "X-XSRF-TOKEN" header, matching Angular's
/// <c>withXsrfConfiguration()</c> default) for state-changing requests, protecting
/// cookie-authenticated endpoints against CSRF.
/// </summary>
/// <remarks>
/// ASP.NET Core's built-in antiforgery auto-validation only applies to endpoints that bind
/// <c>[FromForm]</c> data — this API is pure JSON, so nothing would be protected without this
/// explicit middleware. It covers both Mediator-routed feature endpoints and the built-in
/// <c>MapIdentityApi</c> endpoints uniformly, since both are plain HTTP requests by the time
/// this middleware runs. Must be registered after <c>UseAuthentication</c>/<c>UseAuthorization</c>
/// so <see cref="HttpContext.User"/> is populated (tokens are bound to the current principal).
/// </remarks>
public class AntiforgeryValidationMiddleware(RequestDelegate next, IAntiforgery antiforgery)
{
    private static readonly HashSet<string> ProtectedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ProtectedMethods.Contains(context.Request.Method) ||
            AntiforgeryExemptPaths.IsExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (!await antiforgery.IsRequestValidAsync(context))
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Antiforgery token validation failed",
            };
            problemDetails.Extensions[ApiErrorExtensions.Error] = new ApiErrorContract(
                Code: "auth.invalid_antiforgery_token",
                Diagnostics: new ApiDiagnosticsContract(context.TraceIdentifier));

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            // pass contentType explicitly: WriteAsJsonAsync overwrites Response.ContentType with
            // "application/json; charset=utf-8" by default, so it must be set via this overload
            // rather than assigning Response.ContentType beforehand.
            await context.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
            return;
        }

        await next(context);
    }
}

public static class AntiforgeryValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseAntiforgeryValidation(this IApplicationBuilder app) =>
        app.UseMiddleware<AntiforgeryValidationMiddleware>();
}
