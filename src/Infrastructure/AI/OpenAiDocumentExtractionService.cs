using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using skestock.Application.Documents.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Models.Options;
using skestock.Application.Documents.Schemas;

namespace skestock.Infrastructure.AI;

public class OpenAiDocumentExtractionService(
    HttpClient httpClient,
    IServiceProvider serviceProvider,
    IOptions<OpenAiApiSettings> options): IDocumentExtractionService
{
    public async Task<TResult> ExtractAsync<TResult>(Stream stream, string mimeType, CancellationToken cancellationToken) where TResult : class
    {
        var schemaFactory = serviceProvider.GetService<IExtractionSchemaFactory<TResult>>()
                            ?? throw new InvalidOperationException(
                                $"No {nameof(IExtractionSchemaFactory<TResult>)} registered for {typeof(TResult).Name}");

        var schema = await schemaFactory.Create(cancellationToken);

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var base64 = Convert.ToBase64String(ms.ToArray());
        
        var requestBody = new JsonObject
        {
            ["model"] = options.Value.Model,
            ["input"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "input_file",
                            ["filename"] = "document",
                            ["file_data"] = $"data:{mimeType};base64,{base64}"
                        },
                        new JsonObject
                        {
                            ["type"] = "input_text",
                            ["text"] = "Extract the structured data from this document."
                        }
                    }
                }
            },
            ["text"] = new JsonObject
            {
                ["format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["name"] = typeof(TResult).Name,
                    ["schema"] = schema,
                    ["strict"] = true
                }
            }
        };
        
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("v1/responses", requestBody, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new TransientExtractionException("OpenAI request failed (network)", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TransientExtractionException("OpenAI request timed out", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (statusCode == 429 || statusCode >= 500)
            {
                throw new TransientExtractionException($"OpenAI returned {statusCode}: {errorBody}");
            }

            // 400 invalid schema/request, 413 file too large, etc. — terminal
            throw new UnprocessableDocumentException($"OpenAI rejected the request: {errorBody}");
        }

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);

        var status = payload?["status"]?.GetValue<string>();
        if (status == "incomplete")
        {
            var reason = payload?["incomplete_details"]?["reason"]?.GetValue<string>() ?? "unknown";
            throw reason switch
            {
                "max_output_tokens" => new UnprocessableDocumentException("Document too large/complex for output schema"),
                "content_filter" => new UnprocessableDocumentException("OpenAI declined the document (content filter)"),
                _ => new TransientExtractionException($"OpenAI response incomplete: {reason}")
            };
        }

        var outputText = payload?["output"]?.AsArray()
            .SelectMany(item => item?["content"]?.AsArray() ?? [])
            .FirstOrDefault(c => c?["type"]?.GetValue<string>() == "output_text")?["text"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new UnprocessableDocumentException("OpenAI returned no extractable output text");
        }

        try
        {
            return JsonSerializer.Deserialize<TResult>(outputText)
                ?? throw new UnprocessableDocumentException("Deserialized extraction result was null");
        }
        catch (JsonException ex)
        {
            throw new UnprocessableDocumentException("OpenAI's response did not match the expected schema", ex);
        }
    }
}
