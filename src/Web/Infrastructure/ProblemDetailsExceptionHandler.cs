using System.Text.Json;
using skestock.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace skestock.Web.Infrastructure;

/// <summary>
/// Converts well-known application exceptions into RFC 9110-compliant <see cref="ProblemDetails"/> responses,
/// mapping <see cref="ValidationException"/> → 400, <see cref="NotFoundException"/> → 404,
/// <see cref="UnauthorizedAccessException"/> → 401, and <see cref="ForbiddenAccessException"/> → 403.
/// Unrecognised exceptions are not handled and fall through to the default middleware.
/// </summary>
public class ProblemDetailsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var correlationId = httpContext.TraceIdentifier;

        // validation exceptions are handled specially because they contain a dictionary of errors that can be serialized directly into a ValidationProblemDetails object.
        if (exception is ValidationException ve)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            var validationProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Validation failed"
            };

            validationProblemDetails.Extensions[ApiErrorExtensions.Error] = new ApiErrorContract(
                Code: "validation.failed",
                Errors: ve.Errors
                    .SelectMany(kvp => kvp.Value.Select(fieldError => new ApiErrorItemContract(
                        Field: kvp.Key,
                        Code: fieldError.Code,
                        Params: fieldError.Params)))
                    .ToList(),
                Diagnostics: new ApiDiagnosticsContract(correlationId));

            // pass contentType explicitly: WriteAsJsonAsync overwrites Response.ContentType with
            // "application/json; charset=utf-8" by default, so it must be set via this overload rather
            // than assigning Response.ContentType beforehand.
            await httpContext.Response.WriteAsJsonAsync(validationProblemDetails, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
            return true;
        }

        if (exception is BadHttpRequestException badHttpRequestException)
        {
            var badRequestStatusCode = badHttpRequestException.StatusCode;
            httpContext.Response.StatusCode = badRequestStatusCode;

            var badRequestProblemDetails = new ProblemDetails
            {
                Status = badRequestStatusCode,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Invalid request body"
            };

            var jsonException = badHttpRequestException.InnerException as JsonException;
            var jsonField = NormalizeJsonPath(jsonException?.Path);

            var isJsonBindingError = badHttpRequestException.InnerException is null || badHttpRequestException.InnerException is JsonException;
            var errorCode = isJsonBindingError ? "validation.invalid_json" : "validation.invalid_request";

            badRequestProblemDetails.Extensions[ApiErrorExtensions.Error] = new ApiErrorContract(
                Code: errorCode,
                Errors: BuildBadRequestErrors(badHttpRequestException.InnerException, jsonField),
                Diagnostics: new ApiDiagnosticsContract(correlationId));

            await httpContext.Response.WriteAsJsonAsync(badRequestProblemDetails, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
            return true;
        }

        // other well-known exceptions are handled by mapping them to a status code and a ProblemDetails object.
        var (statusCode, problemDetails) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "Resource not found"
            }),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
            }),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4"
            }),
            _ => (StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Detail = exception.Message
            })
        };

        problemDetails.Extensions[ApiErrorExtensions.Error] = new ApiErrorContract(
            Code: exception switch
            {
                NotFoundException => "common.not_found",
                UnauthorizedAccessException => "auth.unauthorized",
                ForbiddenAccessException => "auth.forbidden",
                _ => "common.unexpected"
            },
            Diagnostics: new ApiDiagnosticsContract(correlationId));

        httpContext.Response.StatusCode = statusCode;
        // pass contentType explicitly: WriteAsJsonAsync overwrites Response.ContentType with
        // "application/json; charset=utf-8" by default, so it must be set via this overload rather
        // than assigning Response.ContentType beforehand.
        await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }

    private static string? NormalizeJsonPath(string? jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
            return null;

        const string rootPrefix = "$.";
        return jsonPath.StartsWith(rootPrefix, StringComparison.Ordinal)
            ? jsonPath[rootPrefix.Length..]
            : jsonPath;
    }

    private static IReadOnlyList<ApiErrorItemContract>? BuildBadRequestErrors(Exception? innerException, string? jsonField)
    {
        if (innerException is JsonException)
        {
            return jsonField is null
                ? [new ApiErrorItemContract(Field: null, Code: "validation.invalid_json")]
                : [new ApiErrorItemContract(Field: jsonField, Code: "validation.invalid_type")];
        }

        if (innerException is FormatException)
            return [new ApiErrorItemContract(Field: null, Code: "validation.invalid_format")];

        if (innerException is OverflowException)
            return [new ApiErrorItemContract(Field: null, Code: "validation.out_of_range")];

        return [new ApiErrorItemContract(Field: null, Code: "validation.invalid_request")];
    }
}
