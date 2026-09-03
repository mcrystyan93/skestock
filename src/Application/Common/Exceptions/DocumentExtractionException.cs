namespace skestock.Application.Common.Exceptions;

/// <summary>
/// Base type for extraction failures. Not thrown directly — use a subtype
/// so callers are forced to be explicit about retryability.
/// </summary>
public abstract class DocumentExtractionException : Exception
{
    protected DocumentExtractionException(string message, Exception? inner = null)
        : base(message, inner) { }
}
