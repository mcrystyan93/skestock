using skestock.Application.Features.FruitDessertTemplates.Models;

namespace skestock.Application.Features.FruitDessertTemplates.Queries.GetMessageTemplates;

// Not ICacheableQuery: two tiny rows, and the editor must see its own save immediately.
public record GetMessageTemplatesQuery : IRequest<Result<MessageTemplatesDto>>;
