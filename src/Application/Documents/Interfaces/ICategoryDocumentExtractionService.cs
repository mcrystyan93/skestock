namespace skestock.Application.Documents.Interfaces;

public interface ICategoryDocumentExtractionService
{
    Task<TResult> ExtractAsync<TResult>(Stream stream, string mimeType,
        CancellationToken cancellationToken) where TResult : class;
}
