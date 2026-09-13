namespace skestock.Application.Documents.Models;

/// <summary>
/// One file to submit as part of a multi-file document extraction request. <see cref="FileName"/> is
/// sent to the model as the <c>input_file</c> content's name so it can distinguish between the files
/// in its response (e.g. when merging suggestions from several pages/documents into one result).
/// </summary>
/// <param name="Stream">The file content. The caller owns disposal.</param>
/// <param name="MimeType">The file's content type (e.g. <c>application/pdf</c>).</param>
/// <param name="FileName">A human-readable name identifying this file to the model.</param>
public sealed record DocumentExtractionInput(Stream Stream, string MimeType, string FileName);
