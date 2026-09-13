using skestock.Application.Documents.Models;

namespace skestock.Application.Documents.Interfaces;

public interface IItemDocumentExtractionService
{
    Task<TResult> ExtractAsync<TResult>(Stream stream, string mimeType,
        CancellationToken cancellationToken) where TResult : class;

    /// <summary>
    /// Extracts from several files in a single request: all files are attached to one call so the
    /// model can merge suggestions across them into a single <typeparamref name="TResult"/>.
    /// </summary>
    Task<TResult> ExtractAsync<TResult>(IReadOnlyList<DocumentExtractionInput> files,
        CancellationToken cancellationToken) where TResult : class;
}
