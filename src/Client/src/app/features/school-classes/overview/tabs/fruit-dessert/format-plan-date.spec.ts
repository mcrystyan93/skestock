import { formatPlanDate } from './format-plan-date';

describe('formatPlanDate', () => {
  it('formats a DateOnly string as weekday, day, month and year in Romanian', () => {
    expect(formatPlanDate('2026-11-10')).toBe('Marți, 10 Noiembrie 2026');
  });

  it('does not shift the day with the timezone', () => {
    expect(formatPlanDate('2026-11-09')).toBe('Luni, 9 Noiembrie 2026');
    expect(formatPlanDate('2027-01-01')).toBe('Vineri, 1 Ianuarie 2027');
  });
});
