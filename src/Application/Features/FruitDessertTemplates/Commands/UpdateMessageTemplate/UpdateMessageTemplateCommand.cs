using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertTemplates.Commands.UpdateMessageTemplate;

// Templates are global (no class) and uncached, so no IRequiresOpenClass / ICacheInvalidation.
public record UpdateMessageTemplateCommand(FruitDessertMessageType Type, string Name, string Body)
    : IRequest<Result>;
