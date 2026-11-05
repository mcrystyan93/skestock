using skestock.Application.Common.Interfaces;
using skestock.Application.Features.FruitDessertTemplates.Models;

namespace skestock.Application.Features.FruitDessertTemplates.Queries.GetMessageTemplates;

public class GetMessageTemplatesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMessageTemplatesQuery, Result<MessageTemplatesDto>>
{
    public async ValueTask<Result<MessageTemplatesDto>> Handle(
        GetMessageTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await dbContext.MessageTemplates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Type)
            .Select(t => new MessageTemplateDto(t.Type, t.Name, t.Body, t.LastModifiedDate))
            .ToListAsync(cancellationToken);

        var placeholders = MessagePlaceholders.All
            .Select(p => new MessagePlaceholderDto(p.Token, p.Label, p.Example))
            .ToList();

        return Result.Ok(new MessageTemplatesDto(templates, placeholders));
    }
}
