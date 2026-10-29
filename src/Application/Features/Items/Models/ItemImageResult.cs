namespace skestock.Application.Features.Items.Models;

/// <summary>Image bytes to stream. IsFallback marks a thumb request answered with the original (thumbnail not generated yet).</summary>
public sealed record ItemImageResult(Stream Content, string ContentType, bool IsFallback);
