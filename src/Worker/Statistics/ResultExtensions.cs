using FluentResults;

namespace Worker.Statistics;

internal static class ResultExtensions
{
    public static string JoinErrorMessages(this IResultBase result) =>
        string.Join("; ", result.Errors.Select(error => error.Message));

    /// <summary>
    /// Turns a failed Mediator result into an exception so the scheduler's retry handling
    /// treats it the same as any other failed attempt.
    /// </summary>
    public static void ThrowIfFailed(this IResultBase result)
    {
        if (result.IsFailed)
        {
            throw new InvalidOperationException(result.JoinErrorMessages());
        }
    }
}
