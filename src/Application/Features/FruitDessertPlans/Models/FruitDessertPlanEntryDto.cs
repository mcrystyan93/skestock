namespace skestock.Application.Features.FruitDessertPlans.Models;

public record FruitDessertPlanEntryDto(
    Guid Id,
    DateOnly Date,
    Guid? CongregationId,
    string? CongregationName,
    bool CongregationDeleted,
    string? ContactPerson,
    string? ContactPhone,
    string? NormalizedPhone,
    bool EmailSent,
    bool ReminderSent,
    bool ThanksSent);
