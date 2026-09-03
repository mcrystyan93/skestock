using System.Text.Json.Nodes;

namespace skestock.Application.Documents.Schemas;

public interface IExtractionSchemaFactory<TResult> where TResult : class
{
    /// <summary>
    /// The provider-neutral, canonical JSON Schema describing <typeparamref name="TResult"/>.
    /// Providers consume it directly (Nutrient) or adapt it to their native schema type (Gemini).
    /// </summary>
    Task<JsonObject> Create(CancellationToken cancellationToken);

    /// <summary>
    /// The extraction prompt/instruction for this document type. Providers that take a text
    /// prompt (e.g. Gemini) use it; providers that infer structure from the schema alone
    /// (e.g. Nutrient's agentic parse) may ignore it.
    /// </summary>
    string Prompt { get; }
}
