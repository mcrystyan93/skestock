using System.Globalization;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertPlans.Queries.GetInformarePdf;

public class GetInformarePdfQueryHandler(
    IApplicationDbContext dbContext,
    IMessageTemplateRenderer renderer,
    IInformarePdfRenderer pdfRenderer)
    : IRequestHandler<GetInformarePdfQuery, Result<FileExportResult>>
{
    public async ValueTask<Result<FileExportResult>> Handle(GetInformarePdfQuery request, CancellationToken cancellationToken)
    {
        var entry = await dbContext.FruitDessertPlanEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == request.EntryId && e.SchoolClassId == request.ClassId, cancellationToken);
        if (entry is null)
            return Result.Fail(new FruitDessertErrors.EntryNotFound(request.EntryId));

        // A congregation deleted after the commitment was made is still valid here, as in Send.
        var congregation = entry.CongregationId is { } congregationId
            ? await dbContext.Congregations.IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == congregationId, cancellationToken)
            : null;
        if (congregation is null || string.IsNullOrWhiteSpace(congregation.DessertContactPerson))
            return Result.Fail(new FruitDessertErrors.ContactMissing(entry.Id));

        var template = await dbContext.MessageTemplates
            .AsNoTracking()
            .Where(t => t.Type == FruitDessertMessageType.Informare && t.IsActive)
            .OrderByDescending(t => t.CreatedDate)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (template is null)
            return Result.Fail(new FruitDessertErrors.ActiveTemplateMissing(nameof(FruitDessertMessageType.Informare)));

        var className = await dbContext.SchoolClasses
            .Where(c => c.Id == request.ClassId)
            .Select(c => c.Name)
            .SingleAsync(cancellationToken);

        var body = renderer.Render(template.Body, new MessageTemplateValues(
            congregation.DessertContactPerson, entry.Date, congregation.Name, className));
        if (body.IsFailed)
            return body.ToResult<FileExportResult>();

        return Result.Ok(new FileExportResult
        {
            Content = pdfRenderer.Render(template.Name, body.Value),
            FileName = $"Informare-fructe-si-desert-{entry.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.pdf",
            ContentType = "application/pdf"
        });
    }
}
