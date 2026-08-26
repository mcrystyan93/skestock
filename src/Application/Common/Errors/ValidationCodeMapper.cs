using FluentValidation.Results;
using skestock.Application.Common.Exceptions;

namespace skestock.Application.Common.Errors;

/// <summary>
/// Maps a FluentValidation <see cref="ValidationFailure"/> to a <see cref="ValidationFieldError"/>
/// with a stable, machine-readable code and camelCase interpolation params for frontend use.
/// </summary>
public static class ValidationCodeMapper
{
    /// <summary>
    /// Maps FluentValidation's built-in validator class names to stable error codes.
    /// When a validator already carries an explicit code (set via <c>.WithErrorCode()</c>),
    /// the explicit code is used directly and this map is not consulted.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> BuiltInCodeMap =
        new Dictionary<string, string>
        {
            ["NotEmptyValidator"] = ValidationErrorCodes.Required,
            ["NotNullValidator"] = ValidationErrorCodes.Required,
            ["MaximumLengthValidator"] = ValidationErrorCodes.MaxLength,
            ["MinimumLengthValidator"] = ValidationErrorCodes.MinLength,
            ["EmailAddressValidator"] = ValidationErrorCodes.Email,
            ["EnumValidator"] = ValidationErrorCodes.InvalidEnum,
            ["GreaterThanValidator"] = ValidationErrorCodes.GreaterThan,
            ["GreaterThanOrEqualValidator"] = ValidationErrorCodes.GreaterThanOrEqualTo,
            ["LessThanValidator"] = ValidationErrorCodes.LessThan,
            ["LessThanOrEqualValidator"] = ValidationErrorCodes.LessThanOrEqualTo,
            ["InclusiveBetweenValidator"] = ValidationErrorCodes.Between,
            ["ExclusiveBetweenValidator"] = ValidationErrorCodes.Between,
        };

    private static readonly IReadOnlyDictionary<string, object> EmptyParams =
        new Dictionary<string, object>();

    /// <summary>
    /// Converts a <see cref="ValidationFailure"/> to a <see cref="ValidationFieldError"/>
    /// with a stable code and extracted interpolation params.
    /// </summary>
    public static ValidationFieldError Map(ValidationFailure failure)
    {
        var rawCode = failure.ErrorCode ?? string.Empty;

        // If ErrorCode does NOT look like a FV built-in (e.g. already "validation.required"),
        // use it as-is. Otherwise, map from the built-in name to a stable code.
        var stableCode = BuiltInCodeMap.TryGetValue(rawCode, out var mapped)
            ? mapped
            : (string.IsNullOrWhiteSpace(rawCode) ? ValidationErrorCodes.Unknown : rawCode);

        var @params = ExtractParams(failure.FormattedMessagePlaceholderValues);
        return new ValidationFieldError(stableCode, @params);
    }

    /// <summary>
    /// Extracts semantically relevant, camelCase-keyed interpolation params
    /// from FluentValidation's <c>FormattedMessagePlaceholderValues</c>.
    /// </summary>
    private static IReadOnlyDictionary<string, object> ExtractParams(IDictionary<string, object>? placeholders)
    {
        if (placeholders is null || placeholders.Count == 0)
            return EmptyParams;

        if (placeholders.ContainsKey("MaxLength"))
            return TryExtract(placeholders, ("MaxLength", "maxLength"));

        if (placeholders.ContainsKey("MinLength"))
            return TryExtract(placeholders, ("MinLength", "minLength"));

        if (placeholders.ContainsKey("ComparisonValue"))
            return TryExtract(placeholders, ("ComparisonValue", "comparisonValue"));

        if (placeholders.ContainsKey("From") || placeholders.ContainsKey("To"))
            return TryExtract(placeholders, ("From", "from"), ("To", "to"));

        return EmptyParams;
    }

    private static IReadOnlyDictionary<string, object> TryExtract(
        IDictionary<string, object> placeholders,
        params (string FvKey, string StableKey)[] mappings)
    {
        var result = new Dictionary<string, object>(mappings.Length);
        foreach (var (fvKey, stableKey) in mappings)
        {
            if (placeholders.TryGetValue(fvKey, out var value) && value is not null)
                result[stableKey] = value;
        }
        return result.Count > 0 ? result : EmptyParams;
    }
}




