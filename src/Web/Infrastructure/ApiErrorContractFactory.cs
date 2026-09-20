using System.Diagnostics;

namespace skestock.Web.Infrastructure;

/// <summary>
/// Single construction point for the <see cref="ApiErrorContract"/> payload shared by the two
/// error-handling paths (<see cref="ProblemDetailsExceptionHandler"/> for thrown exceptions and
/// <see cref="ResultProblemDetailsMapper"/> for failed <c>Result</c> values). Centralising it here
/// guarantees both paths emit an identical contract and a single correlation-id policy.
/// </summary>
internal static class ApiErrorContractFactory
{
    /// <summary>
    /// Resolves the correlation id, preferring the ambient trace (<see cref="Activity.Current"/>)
    /// and falling back to the request's <see cref="HttpContext.TraceIdentifier"/> when available.
    /// </summary>
    public static string? ResolveCorrelationId(HttpContext? httpContext = null)
        => Activity.Current?.Id ?? httpContext?.TraceIdentifier;

    public static ApiErrorContract Create(
        string code,
        IReadOnlyList<ApiErrorItemContract>? errors = null,
        HttpContext? httpContext = null)
        => new(code, errors, new ApiDiagnosticsContract(ResolveCorrelationId(httpContext)));
}
