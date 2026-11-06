namespace skestock.Application.Features.FruitDessertPlans.Abstractions;

public interface IWorkingDayProvider
{
    /// <summary>Working days in the inclusive interval; empty when <paramref name="end"/> precedes <paramref name="start"/>.</summary>
    IEnumerable<DateOnly> GetWorkingDays(DateOnly start, DateOnly end);
}
