using skestock.Application.Common.Security;
using skestock.Domain.Constants;
using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertTemplates.Commands.UpdateMessageTemplate;

// Templates are global (no class) and uncached, so no IRequiresOpenClass / ICacheInvalidation.
[Authorize(Roles = Roles.Administrator)]
public record UpdateMessageTemplateCommand(FruitDessertMessageType Type, string Name, string Body)
    : IRequest<Result>;
