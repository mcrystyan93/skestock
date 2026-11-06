using skestock.Application.Features.FruitDessertPlans.Abstractions;

namespace skestock.Infrastructure.FruitDessert;

// shortcut: Monday to Friday only, no public holidays; add them here when needed.
public class WorkingDayProvider : IWorkingDayProvider
{
    public IEnumerable<DateOnly> GetWorkingDays(DateOnly start, DateOnly end)
    {
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                yield return day;
            }
        }
    }
}
