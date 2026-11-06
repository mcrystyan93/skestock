using skestock.Application.Common.Interfaces;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Application.Features.FruitDessertPlans.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertPlans;

// Shared by the listing and the commands, so every response carries the same live contact data.
internal static class FruitDessertPlanProjection
{
    // Member-initialised (not positional) so EF can translate filters applied after the projection.
    internal sealed class Row
    {
        public Guid Id { get; init; }
        public Guid SchoolClassId { get; init; }
        public DateOnly Date { get; init; }
        public Guid? CongregationId { get; init; }
        public string? CongregationName { get; init; }
        public bool CongregationDeleted { get; init; }
        public string? ContactPerson { get; init; }
        public string? ContactPhone { get; init; }
        public bool EmailSent { get; init; }
        public bool ReminderSent { get; init; }
        public bool ThanksSent { get; init; }
    }

    // Soft-deleted congregations stay visible: the planning commitment outlives the deletion.
    public static IQueryable<Row> Rows(IApplicationDbContext dbContext) =>
        from e in dbContext.FruitDessertPlanEntries.AsNoTracking()
        join c in dbContext.Congregations.IgnoreQueryFilters() on e.CongregationId equals c.Id into cs
        from c in cs.DefaultIfEmpty()
        select new Row
        {
            Id = e.Id,
            SchoolClassId = e.SchoolClassId,
            Date = e.Date,
            CongregationId = e.CongregationId,
            CongregationName = c == null ? null : c.Name,
            CongregationDeleted = c != null && c.DeletedDate != null,
            ContactPerson = c == null ? null : c.DessertContactPerson,
            ContactPhone = c == null ? null : c.DessertContactPhone,
            EmailSent = e.EmailSent,
            ReminderSent = e.Messages.Any(m => m.Type == FruitDessertMessageType.Reminder),
            ThanksSent = e.Messages.Any(m => m.Type == FruitDessertMessageType.Thanks)
        };

    public static FruitDessertPlanEntryDto ToDto(Row row, IPhoneNumberNormalizer phoneNumberNormalizer)
    {
        var normalized = phoneNumberNormalizer.Normalize(row.ContactPhone);
        return new FruitDessertPlanEntryDto(
            row.Id, row.Date, row.CongregationId, row.CongregationName, row.CongregationDeleted,
            row.ContactPerson, row.ContactPhone, normalized.IsSuccess ? normalized.Value : null,
            row.EmailSent, row.ReminderSent, row.ThanksSent);
    }
}
