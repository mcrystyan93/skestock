namespace skestock.Application.Common.Caching;

/// <summary>
/// Serializable result cache container that mirrors FluentResults.Result{T} structure
/// but without the complex internal state that prevents serialization by HybridCache.
/// </summary>
/// <typeparam name="T">The cached value type</typeparam>
public class ResultCache<T>
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The cached value (only populated if IsSuccess is true).
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// List of errors if the operation failed.
    /// </summary>
    public List<CachedError> Errors { get; set; } = new();

    public ResultCache() { }

    public ResultCache(bool isSuccess, T? value = default, List<CachedError>? errors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors ?? new();
    }
}

/// <summary>
/// Serializable representation of a FluentResults.Error.
/// </summary>
public class CachedError
{
    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified type name of the original error class (for reconstruction if needed).
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    public CachedError() { }

    public CachedError(string message, string errorType)
    {
        Message = message;
        ErrorType = errorType;
    }
}
