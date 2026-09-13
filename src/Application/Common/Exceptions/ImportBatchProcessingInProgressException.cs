namespace skestock.Application.Common.Exceptions;

/// <summary>
/// Indicates that another queue delivery currently owns the processing lease for an import batch.
/// The queue message must remain available so it can be retried after the active delivery finishes.
/// </summary>
public sealed class ImportBatchProcessingInProgressException(Guid batchId)
    : Exception($"Import batch '{batchId}' is already being processed.");
