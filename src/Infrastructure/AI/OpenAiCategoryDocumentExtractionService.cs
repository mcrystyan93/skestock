using skestock.Application.Documents.Interfaces;
using skestock.Application.Documents.Models;

namespace skestock.Infrastructure.AI;

public sealed class OpenAiCategoryDocumentExtractionService(OpenAiDocumentExtractionClient extractionClient)
    : ICategoryDocumentExtractionService
{
    public Task<TResult> ExtractAsync<TResult>(
        Stream stream,
        string mimeType,
        CancellationToken cancellationToken) where TResult : class =>
        extractionClient.ExtractAsync<TResult>(stream, mimeType, cancellationToken);

    public Task<TResult> ExtractAsync<TResult>(
        IReadOnlyList<DocumentExtractionInput> files,
        CancellationToken cancellationToken) where TResult : class =>
        extractionClient.ExtractAsync<TResult>(files, cancellationToken);
}
