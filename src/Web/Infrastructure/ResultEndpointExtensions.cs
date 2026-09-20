using FluentResults;
using Microsoft.AspNetCore.Http.HttpResults;

namespace skestock.Web.Infrastructure;

/// <summary>
/// Bridges FluentResults <see cref="Result"/>/<see cref="Result{T}"/> to minimal-API
/// <see cref="Results{TResult1, TResult2}"/> return values, so endpoints express the
/// success shape once instead of repeating the <c>if (result.IsFailed) …</c> guard.
/// Failures are routed through <see cref="ResultProblemDetailsMapper.ToProblemHttpResult"/>,
/// keeping the wire contract identical to the hand-written form.
/// </summary>
public static class ResultEndpointExtensions
{
    public static Results<Ok<T>, ProblemHttpResult> ToOk<T>(this Result<T> result)
        => result.IsFailed
            ? result.ToProblemHttpResult()
            : TypedResults.Ok(result.Value);

    public static Results<Ok<TOut>, ProblemHttpResult> ToOk<T, TOut>(
        this Result<T> result, Func<T, TOut> selector)
        => result.IsFailed
            ? result.ToProblemHttpResult()
            : TypedResults.Ok(selector(result.Value));

    public static Results<Ok, ProblemHttpResult> ToOk(this Result result)
        => result.IsFailed
            ? result.ToProblemHttpResult()
            : TypedResults.Ok();

    public static Results<NoContent, ProblemHttpResult> ToNoContent(this Result result)
        => result.IsFailed
            ? result.ToProblemHttpResult()
            : TypedResults.NoContent();

    public static Results<Created<T>, ProblemHttpResult> ToCreated<T>(
        this Result<T> result, Func<T, string> location)
        => result.IsFailed
            ? result.ToProblemHttpResult()
            : TypedResults.Created(location(result.Value), result.Value);
}
