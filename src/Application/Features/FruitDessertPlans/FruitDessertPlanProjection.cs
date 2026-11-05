using skestock.Application.Common.Interfaces;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Application.Features.FruitDessertPlans.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertPlans;

// Shared by the listing and the commands, so every response carries the same live contact data.
internal static class FruitDessertPlanProjection
{
    internal sealed record Row(
        Guid Id,
        Guid SchoolClassId,
        DateOnly Date,
        Guid? CongregationId,
        string? CongregationName,
        bool CongregationDeleted,
        string? ContactPerson,
        string? ContactPhone,
        bool EmailSent,
        bool ReminderSent,
        bool ThanksSent);

    // Soft-deleted congregations stay visible: the planning commitment outlives the deletion.
    public static IQueryable<Row> Rows(IApplicationDbContext dbContext) =>
        from e in dbContext.FruitDessertPlanEntries.AsNoTracking()
        join c in dbContext.Congregations.IgnoreQueryFilters() on e.CongregationId equals c.Id into cs
        from c in cs.DefaultIfEmpty()
        select new Row(
            e.Id,
            e.SchoolClassId,
            e.Date,
            e.CongregationId,
            c == null ? null : c.Name,
            c != null && c.DeletedDate != null,
            c == null ? null : c.DessertContactPerson,
            c == null ? null : c.DessertContactPhone,
            e.EmailSent,
            e.Messages.Any(m => m.Type == FruitDessertMessageType.Reminder),
            e.Messages.Any(m => m.Type == FruitDessertMessageType.Thanks));

    public static FruitDessertPlanEntryDto ToDto(Row row, IPhoneNumberNormalizer phoneNumberNormalizer)
    {
        var normalized = phoneNumberNormalizer.Normalize(row.ContactPhone);
        return new FruitDessertPlanEntryDto(
            row.Id, row.Date, row.CongregationId, row.CongregationName, row.CongregationDeleted,
            row.ContactPerson, row.ContactPhone, normalized.IsSuccess ? normalized.Value : null,
            row.EmailSent, row.ReminderSent, row.ThanksSent);
    }
}
