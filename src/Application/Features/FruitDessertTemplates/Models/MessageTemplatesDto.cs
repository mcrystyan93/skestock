using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertTemplates.Models;

public record MessageTemplatesDto(
    IReadOnlyList<MessageTemplateDto> Templates,
    IReadOnlyList<MessagePlaceholderDto> Placeholders);

public record MessageTemplateDto(FruitDessertMessageType Type, string Name, string Body, DateTimeOffset LastModified);

public record MessagePlaceholderDto(string Token, string Label, string Example);
