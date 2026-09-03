namespace skestock.Application.Common.Exceptions;

/// <summary>
/// The extraction call itself failed for operational reasons (network blip,
/// rate limit, timeout, 5xx from Gemini). Safe to retry — the same input
/// might succeed on a later attempt.
/// </summary>
public class TransientExtractionException : DocumentExtractionException
{
    public TransientExtractionException(string message, Exception? inner = null)
        : base(message, inner) { }
}
