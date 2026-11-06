using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.FruitDessertTemplates.Commands.UpdateMessageTemplate;

public class UpdateMessageTemplateCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateMessageTemplateCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateMessageTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await dbContext.MessageTemplates
            .Where(t => t.Type == request.Type && t.IsActive)
            .OrderByDescending(t => t.CreatedDate)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (template is null)
            return Result.Fail(new FruitDessertErrors.ActiveTemplateMissing(request.Type.ToString()));

        template.Name = request.Name;
        template.Body = WhatsAppFormatter.Sanitize(request.Body);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
