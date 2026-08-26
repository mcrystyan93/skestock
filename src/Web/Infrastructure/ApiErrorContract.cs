namespace skestock.Web.Infrastructure;

/// <summary>
/// Unified code-first error payload attached to <see cref="ProblemDetails.Extensions"/>.
/// Frontend maps codes to locale-specific copy.
/// </summary>
public static class ApiErrorExtensions
{
    public const string Error = "error";
}

public sealed record ApiErrorContract(
    string Code,
    IReadOnlyList<ApiErrorItemContract>? Errors = null,
    ApiDiagnosticsContract? Diagnostics = null);

public sealed record ApiErrorItemContract(
    string? Field,
    string Code,
    IReadOnlyDictionary<string, object>? Params = null);

public sealed record ApiDiagnosticsContract(string? CorrelationId);

