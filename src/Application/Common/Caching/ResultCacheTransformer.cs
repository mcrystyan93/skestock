using System.Reflection;
using System.Text.Json;

namespace skestock.Application.Common.Caching;

/// <summary>
/// Extension methods to transform between FluentResults.Result{T} and ResultCache{T}.
/// </summary>
public static class ResultCacheTransformer
{
    private const string ResultTypeName = "FluentResults.Result`1";

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

    public static string Serialize(object result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var resultType = result.GetType();
        if (!resultType.IsGenericType ||
            resultType.GetGenericTypeDefinition().FullName != ResultTypeName)
        {
            throw new InvalidOperationException(
                $"Caching requires a response of type {ResultTypeName}, but received {resultType.FullName}.");
        }

        var valueType = resultType.GetGenericArguments()[0];
        var serializeMethod = typeof(ResultCacheTransformer)
            .GetMethod(nameof(SerializeResult), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(valueType);

        return (string)serializeMethod.Invoke(null, [result])!;
    }

    public static TResponse Deserialize<TResponse>(string serialized)
    {
        ArgumentNullException.ThrowIfNull(serialized);

        var responseType = typeof(TResponse);
        if (!responseType.IsGenericType ||
            responseType.GetGenericTypeDefinition().FullName != ResultTypeName)
        {
            throw new InvalidOperationException(
                $"Caching requires a response of type {ResultTypeName}, but received {responseType.FullName}.");
        }

        var valueType = responseType.GetGenericArguments()[0];
        var deserializeMethod = typeof(ResultCacheTransformer)
            .GetMethod(nameof(DeserializeResult), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(valueType);

        return (TResponse)deserializeMethod.Invoke(null, [serialized])!;
    }

    private static string SerializeResult<T>(Result<T> result) =>
        JsonSerializer.Serialize(result.ToResultCache());

    private static Result<T> DeserializeResult<T>(string serialized) =>
        JsonSerializer.Deserialize<ResultCache<T>>(serialized)?.ToResult()
        ?? throw new InvalidOperationException("The cached result payload was null.");
}
