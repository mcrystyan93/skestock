using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Models.Options;
using skestock.Application.Documents.Models;
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
        var schemaFactory = GetSchemaFactory<TResult>();
        var schema = await schemaFactory.Create(cancellationToken);
        var base64 = await ToBase64Async(stream, cancellationToken);

        var content = new JsonArray
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
        };

        var requestBody = BuildRequestBody<TResult>(content, schema);

        return await SendAsync<TResult>(requestBody, cancellationToken);
    }

    /// <summary>
    /// Extracts from several files in one Responses API call: each file becomes its own named
    /// <c>input_file</c> content item alongside a single shared <c>input_text</c> prompt, so the
    /// model sees every file in the same turn and can merge its suggestions into one
    /// <typeparamref name="TResult"/>. Preserves the same request/response shape as the single-file
    /// overload above - only the number of <c>input_file</c> content items differs.
    /// </summary>
    public async Task<TResult> ExtractAsync<TResult>(
        IReadOnlyList<DocumentExtractionInput> files,
        CancellationToken cancellationToken) where TResult : class
    {
        Guard.Against.NullOrEmpty(files, message: "At least one file is required for extraction.");

        var schemaFactory = GetSchemaFactory<TResult>();
        var schema = await schemaFactory.Create(cancellationToken);

        var content = new JsonArray();
        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var base64 = await ToBase64Async(file.Stream, cancellationToken);
            var filename = string.IsNullOrWhiteSpace(file.FileName) ? $"document-{i + 1}" : file.FileName;

            content.Add(new JsonObject
            {
                ["type"] = "input_file",
                ["filename"] = filename,
                ["file_data"] = $"data:{file.MimeType};base64,{base64}"
            });
        }

        content.Add(new JsonObject
        {
            ["type"] = "input_text",
            ["text"] = schemaFactory.Prompt
        });

        var requestBody = BuildRequestBody<TResult>(content, schema);

        return await SendAsync<TResult>(requestBody, cancellationToken);
    }

    private IExtractionSchemaFactory<TResult> GetSchemaFactory<TResult>() where TResult : class =>
        serviceProvider.GetService<IExtractionSchemaFactory<TResult>>()
        ?? throw new InvalidOperationException(
            $"No {nameof(IExtractionSchemaFactory<TResult>)} registered for {typeof(TResult).Name}");

    private static async Task<string> ToBase64Async(Stream stream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    private JsonObject BuildRequestBody<TResult>(JsonArray content, JsonObject schema) where TResult : class =>
        new()
        {
            ["model"] = options.Value.Model,
            ["input"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = content
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

    private async Task<TResult> SendAsync<TResult>(JsonObject requestBody, CancellationToken cancellationToken)
        where TResult : class
    {
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
                .FirstOrDefault(contentItem => contentItem?["type"]?.GetValue<string>() == "output_text")
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
