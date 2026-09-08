using skestock.Application.Documents.Interfaces;

namespace skestock.Infrastructure.AI;

public sealed class OpenAiStockDocumentExtractionService(OpenAiDocumentExtractionClient extractionClient)
    : IStockDocumentExtractionService
{
    public Task<TResult> ExtractAsync<TResult>(
        Stream stream,
        string mimeType,
        CancellationToken cancellationToken) where TResult : class =>
        extractionClient.ExtractAsync<TResult>(stream, mimeType, cancellationToken);
}
