namespace skestock.Application.Common.Exceptions;

/// <summary>
/// The call succeeded but the *result* is unusable for a reason that won't
/// change on retry — file is corrupt/unreadable, content doesn't match the
/// expected document type, model explicitly declined, schema validation
/// failed against the model's own output. Retrying with identical input
/// will fail identically.
/// </summary>
public class UnprocessableDocumentException : DocumentExtractionException
{
    public UnprocessableDocumentException(string message, Exception? inner = null)
        : base(message, inner) { }
}
