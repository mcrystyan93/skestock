using skestock.Application.Common.Exceptions;
using ValidationException = skestock.Application.Common.Exceptions.ValidationException;

namespace skestock.Application.Queues;

/// <summary>
/// Classifies exceptions raised while dispatching a queued message through the Mediator pipeline
/// as permanent (retrying will never succeed) or transient (a later attempt might succeed).
/// Consumed by queue processors so they can dead-letter permanent failures immediately instead of
/// exhausting their retry budget on something that can never succeed.
/// </summary>
public static class MessageFailureClassifier
{
    public static bool IsPermanent(Exception ex) => ex switch
    {
        ValidationException => true,
        UnauthorizedAccessException => true,
        ForbiddenAccessException => true,
        UnprocessableDocumentException => true,
        ArgumentException => true,
        _ => false
    };
}
