using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Models.Options;
using skestock.Application.Documents.Interfaces;
using skestock.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Documents.Schemas;
using skestock.Infrastructure.AI.Schemas;

namespace skestock.Infrastructure.AI;

public class GeminiDocumentExtractionService(IOptions<GeminiApiSettings> options, IServiceProvider serviceProvider)
    : IDocumentExtractionService
{
    private readonly Client _client = new(apiKey: options.Value.ApiKey);

    public async Task<TResult> ExtractAsync<TResult>(Stream stream, string mimeType,
        CancellationToken cancellationToken) where TResult : class
    {
        var schemaFactory = serviceProvider.GetService<IExtractionSchemaFactory<TResult>>()
                            ?? throw new InvalidOperationException(
                                $"No {nameof(IExtractionSchemaFactory<TResult>)} registered for {typeof(TResult).Name}");

        var config = new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            ResponseSchema = GeminiSchemaTranslator.Translate(await schemaFactory.Create(cancellationToken)),
            Temperature = 0.0f
        };

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);

        var content = new Content
        {
            Parts = new List<Part>
            {
                new Part { InlineData = new Blob { MimeType = mimeType, Data = ms.ToArray() } },
                new Part { Text = schemaFactory.Prompt }
            }
        };

        GenerateContentResponse response;
        try
        {
            response = await _client.Models.GenerateContentAsync(
                model: options.Value.Model,
                contents: content,
                config: config,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            throw new TransientExtractionException(
                "Document extraction failed. There was an error", ex);
        }

        if (string.IsNullOrWhiteSpace(response.Text))
            throw new UnprocessableDocumentException(
                "Document extraction failed. The response from the AI model was empty.");

        if (!StringHelpers.IsValidJson(response.Text))
            throw new UnprocessableDocumentException(
                "Document extraction failed. The response from the AI model was not valid JSON.");

        try
        {
            return JsonSerializer.Deserialize<TResult>(response.Text)
                   ?? throw new UnprocessableDocumentException(
                       "Document extraction failed. The response from the AI model could not be deserialized.");
        }
        catch (JsonException ex)
        {
            throw new UnprocessableDocumentException("Gemini's response did not match the expected schema", ex);
        }
    }
}
