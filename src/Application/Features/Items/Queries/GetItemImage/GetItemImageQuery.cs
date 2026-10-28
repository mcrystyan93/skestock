using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetItemImage;

// Streams a blob, so deliberately not an ICacheableQuery; the endpoint sets HTTP cache headers instead.
public record GetItemImageQuery(Guid ItemId, ItemImageSize Size) : IRequest<Result<ItemImageResult>>;
