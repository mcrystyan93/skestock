using System.Diagnostics;
using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Errors;

namespace skestock.Web.Infrastructure;

/// <summary>
/// Converts failed FluentResults <see cref="ResultBase"/> instances to RFC 9110-compliant
/// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> responses.
/// Uses error metadata (status code/title) when available and aggregates multiple errors.
/// Mirrors the style used by <see cref="ProblemDetailsExceptionHandler"/>.
/// </summary>
public static class ResultProblemDetailsMapper
{
    public static ProblemHttpResult ToProblemHttpResult(this ResultBase result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to a problem result.");
        }

        var statusCodes = result.Errors
            .Select(TryGetStatusCode)
            .Where(code => code.HasValue)
            .Select(code => code!.Value)
            .ToList();

        var statusCode = statusCodes.Count == 0
            ? StatusCodes.Status400BadRequest
            : statusCodes.All(code => code == statusCodes[0])
                ? statusCodes[0]
                : StatusCodes.Status400BadRequest;

        var titles = result.Errors
            .Select(TryGetTitle)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .ToList();

        var title = titles.Count == 0
            ? GetDefaultTitle(statusCode)
            : titles.All(t => string.Equals(t, titles[0], StringComparison.Ordinal))
                ? titles[0]!
                : "One or more errors occurred.";

        var type = GetTypeForStatusCode(statusCode);

        var errorItems = result.Errors
            .Select(error => new ApiErrorItemContract(
                Field: null,
                Code: TryGetCode(error) ?? "common.unexpected",
                Params: TryGetParams(error)))
            .ToList();

        var globalCode = errorItems
            .Select(item => item.Code)
            .Distinct(StringComparer.Ordinal)
            .Count() == 1
                ? errorItems[0].Code
                : "common.operation_failed";

        var problem = TypedResults.Problem(
            statusCode: statusCode,
            title: title,
            type: type);

        problem.ProblemDetails.Extensions[ApiErrorExtensions.Error] = new ApiErrorContract(
            Code: globalCode,
            Errors: errorItems,
            Diagnostics: new ApiDiagnosticsContract(Activity.Current?.Id));

        return problem;
    }

    private static int? TryGetStatusCode(IError error)
    {
        if (!error.Metadata.TryGetValue(ErrorMetadataKeys.StatusCode, out var value))
            return null;

        return value switch
        {
            int statusCode => statusCode,
            _ => null
        };
    }

    private static string? TryGetTitle(IError error)
    {
        if (!error.Metadata.TryGetValue(ErrorMetadataKeys.Title, out var value))
            return null;

        return value as string;
    }

    private static string? TryGetCode(IError error)
    {
        if (!error.Metadata.TryGetValue(ErrorMetadataKeys.Code, out var value))
            return null;

        return value as string;
    }

    private static IReadOnlyDictionary<string, object>? TryGetParams(IError error)
    {
        if (!error.Metadata.TryGetValue(ErrorMetadataKeys.Params, out var value))
            return null;

        return value as IReadOnlyDictionary<string, object>;
    }

    private static string GetDefaultTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status404NotFound => "The specified resource was not found.",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
            _ => "One or more errors occurred."
        };
    }

    private static string GetTypeForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            _ => "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };
    }
}
