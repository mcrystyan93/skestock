using System.Text.Json.Nodes;

namespace skestock.Application.Documents.Schemas;

public interface IExtractionSchemaFactory<TResult> where TResult : class
{
    /// <summary>
    /// The provider-neutral, canonical JSON Schema describing <typeparamref name="TResult"/>.
    /// The document extraction client consumes it when requesting structured output.
    /// </summary>
    Task<JsonObject> Create(CancellationToken cancellationToken);

    /// <summary>
    /// The extraction prompt/instruction for this document type.
    /// </summary>
    string Prompt { get; }
}
