using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Application.Features.FruitDessertPlans.Models;
using skestock.Domain.Entities.FruitDessert;

namespace skestock.Application.Features.FruitDessertPlans.Commands.SendFruitDessertMessage;

public class SendFruitDessertMessageCommandHandler(
    IApplicationDbContext dbContext,
    IMessageTemplateRenderer renderer,
    IPhoneNumberNormalizer phoneNumberNormalizer,
    IMessageSender sender,
    TimeProvider timeProvider)
    : IRequestHandler<SendFruitDessertMessageCommand, Result<FruitDessertMessageDispatchDto>>
{
    public async ValueTask<Result<FruitDessertMessageDispatchDto>> Handle(
        SendFruitDessertMessageCommand request, CancellationToken cancellationToken)
    {
        var entry = await dbContext.FruitDessertPlanEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == request.EntryId && e.SchoolClassId == request.ClassId, cancellationToken);
        if (entry is null)
            return Result.Fail(new FruitDessertErrors.EntryNotFound(request.EntryId));

        // A congregation deleted after the commitment was made still receives the message.
        var congregation = entry.CongregationId is { } congregationId
            ? await dbContext.Congregations.IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == congregationId, cancellationToken)
            : null;
        if (congregation is null
            || string.IsNullOrWhiteSpace(congregation.DessertContactPerson)
            || string.IsNullOrWhiteSpace(congregation.DessertContactPhone))
            return Result.Fail(new FruitDessertErrors.ContactMissing(entry.Id));

        var phone = phoneNumberNormalizer.Normalize(congregation.DessertContactPhone);
        if (phone.IsFailed)
            return Result.Fail(new FruitDessertErrors.InvalidPhone(entry.Id));

        var template = await dbContext.MessageTemplates
            .AsNoTracking()
            .Where(t => t.Type == request.Type && t.IsActive)
            .OrderByDescending(t => t.CreatedDate)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (template is null)
            return Result.Fail(new FruitDessertErrors.ActiveTemplateMissing(request.Type.ToString()));

        var className = await dbContext.SchoolClasses
            .Where(c => c.Id == request.ClassId)
            .Select(c => c.Name)
            .SingleAsync(cancellationToken);

        var body = renderer.Render(template.Body, new MessageTemplateValues(
            congregation.DessertContactPerson, entry.Date, congregation.Name, className));
        if (body.IsFailed)
            return body.ToResult<FruitDessertMessageDispatchDto>();

        var dispatch = await sender.SendAsync(phone.Value, body.Value, cancellationToken);
        if (dispatch.IsFailed)
            return dispatch.ToResult<FruitDessertMessageDispatchDto>();

        var sentAt = timeProvider.GetUtcNow();
        dbContext.FruitDessertMessages.Add(new FruitDessertMessage
        {
            EntryId = entry.Id,
            Type = request.Type,
            Status = dispatch.Value.Status,
            SentAt = sentAt,
            RecipientPhone = phone.Value,
            Body = body.Value,
            TemplateId = template.Id
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new FruitDessertMessageDispatchDto(
            dispatch.Value.Url, request.Type, dispatch.Value.Status, sentAt));
    }
}
