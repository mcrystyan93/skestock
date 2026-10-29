namespace skestock.Application.Storage.Models;

public sealed record BlobContent(Stream Content, string ContentType);
