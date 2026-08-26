namespace skestock.Application.Common.Caching;

/// <summary>
/// Extension methods to transform between FluentResults.Result{T} and ResultCache{T}.
/// </summary>
public static class ResultCacheTransformer
{
    /// <summary>
    /// Transforms a FluentResults Result{T} into a serializable ResultCache{T}.
    /// </summary>
    public static ResultCache<T> ToResultCache<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new ResultCache<T>(isSuccess: true, value: result.Value);
        }

        var cachedErrors = result.Errors
            .Select(error => new CachedError(
                message: error.Message,
                errorType: error.GetType().FullName ?? error.GetType().Name))
            .ToList();

        return new ResultCache<T>(isSuccess: false, errors: cachedErrors);
    }

    /// <summary>
    /// Transforms a ResultCache{T} back into a FluentResults Result{T}.
    /// </summary>
    public static Result<T> ToResult<T>(this ResultCache<T> cached)
    {
        if (cached.IsSuccess)
        {
            return Result.Ok(cached.Value!);
        }

        // Reconstruct failure result from cached errors
        var result = Result.Fail<T>(cached.Errors
            .Select(e => e.Message)
            .ToList());

        return result;
    }
}
