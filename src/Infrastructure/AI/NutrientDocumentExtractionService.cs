using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using skestock.Application.Documents.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Common.Exceptions;
using skestock.Application.Documents.Schemas;
using skestock.Infrastructure.AI.Models;

namespace skestock.Infrastructure.AI;

public class NutrientDocumentExtractionService(
    HttpClient httpClient,
    IServiceProvider serviceProvider) : IDocumentExtractionService
{
    public async Task<TResult> ExtractAsync<TResult>(Stream stream, string mimeType, CancellationToken cancellationToken)
        where TResult : class
    {
        var schemaFactory = serviceProvider.GetService<IExtractionSchemaFactory<TResult>>()
                            ?? throw new InvalidOperationException(
                                $"No {nameof(IExtractionSchemaFactory<TResult>)} registered for {typeof(TResult).Name}");

        var schema = await schemaFactory.Create(cancellationToken);
        
        var instructions = new JsonObject
        {
            ["schema"] = schema,
            ["parseConfig"] = new JsonObject { ["mode"] = "agentic" } // OCR + AI-augmented; adjust per doc type
        };

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        
        content.Add(streamContent, "file", "document"); // filename is arbitrary here
        content.Add(new StringContent(instructions.ToJsonString()), "instructions");
        
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync("extraction/extract", content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new TransientExtractionException("Nutrient request failed (network)", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TransientExtractionException("Nutrient request timed out", ex);
        }

        var body = await response.Content.ReadFromJsonAsync<NutrientExtractResponse>(cancellationToken: cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorMessage = body?.ErrorMessage ?? $"HTTP {statusCode}";

            // 429 rate limit / 5xx → transient, worth retrying
            if (statusCode is 429 or >= 500)
            {
                throw new TransientExtractionException($"Nutrient returned {statusCode}: {errorMessage}");
            }

            // 400 bad schema, 415 unsupported file type, 422 unprocessable document → terminal
            throw new UnprocessableDocumentException($"Nutrient rejected the request: {errorMessage}");
        }
        
        
        if (body?.Output is null)
        {
            throw new UnprocessableDocumentException("Nutrient returned no output data");
        }

        try
        {
            return body.Output.Data.Deserialize<TResult>()
                   ?? throw new UnprocessableDocumentException("Deserialized extraction result was null");
        }
        catch (JsonException ex)
        {
            throw new UnprocessableDocumentException("Nutrient's response did not match the expected schema", ex);
        }

    }
}
