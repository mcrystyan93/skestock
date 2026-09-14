using skestock.Application.Documents.Models;

namespace skestock.Application.Documents.Interfaces;

public interface ICategoryDocumentExtractionService
{
    Task<TResult> ExtractAsync<TResult>(Stream stream, string mimeType,
        CancellationToken cancellationToken) where TResult : class;

    Task<TResult> ExtractAsync<TResult>(IReadOnlyList<DocumentExtractionInput> files,
        CancellationToken cancellationToken) where TResult : class;
}
