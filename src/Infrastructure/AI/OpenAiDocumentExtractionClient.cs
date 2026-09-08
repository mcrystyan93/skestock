using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Models.Options;
using skestock.Application.Documents.Schemas;

namespace skestock.Infrastructure.AI;

public sealed class OpenAiDocumentExtractionClient(
    HttpClient httpClient,
    IServiceProvider serviceProvider,
    IOptions<OpenAiApiSettings> options)
{
    public async Task<TResult> ExtractAsync<TResult>(
        Stream stream,
        string mimeType,
        CancellationToken cancellationToken) where TResult : class
    {
        var schemaFactory = serviceProvider.GetService<IExtractionSchemaFactory<TResult>>()
                            ?? throw new InvalidOperationException(
                                $"No {nameof(IExtractionSchemaFactory<TResult>)} registered for {typeof(TResult).Name}");

        var schema = await schemaFactory.Create(cancellationToken);

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        var base64 = Convert.ToBase64String(memoryStream.ToArray());

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
                            ["text"] = schemaFactory.Prompt
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

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (statusCode == 429 || statusCode >= 500)
                {
                    throw new TransientExtractionException($"OpenAI returned {statusCode}: {errorBody}");
                }

                throw new UnprocessableDocumentException($"OpenAI rejected the request: {errorBody}");
            }

            var payload = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);

            var status = payload?["status"]?.GetValue<string>();
            if (status == "incomplete")
            {
                var reason = payload?["incomplete_details"]?["reason"]?.GetValue<string>() ?? "unknown";
                throw reason switch
                {
                    "max_output_tokens" =>
                        new UnprocessableDocumentException("Document too large/complex for output schema"),
                    "content_filter" =>
                        new UnprocessableDocumentException("OpenAI declined the document (content filter)"),
                    _ => new TransientExtractionException($"OpenAI response incomplete: {reason}")
                };
            }

            var outputText = payload?["output"]?.AsArray()
                .SelectMany(item => item?["content"]?.AsArray() ?? [])
                .FirstOrDefault(content => content?["type"]?.GetValue<string>() == "output_text")
                ?["text"]?.GetValue<string>();

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
}
