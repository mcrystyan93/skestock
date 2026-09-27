import { ClassDailyConsumptionDto } from '@ske/models';
import { summarizeConsumption } from './class-consumption-periods';

const consumption = (points: ClassDailyConsumptionDto['points']): ClassDailyConsumptionDto => ({
  classId: 'class-id',
  fromDate: points[0]?.date ?? null,
  toDate: points.at(-1)?.date ?? null,
  points,
  totalQuantity: points.reduce((sum, point) => sum + point.quantity, 0),
  totalValue: points.reduce((sum, point) => sum + point.value, 0),
  averageQuantity: 1.25,
  averageValue: 2.75
});

describe('summarizeConsumption', () => {
  it('preserves the API daily series and its averages', () => {
    const data = consumption([
      { date: '2026-01-01', quantity: 1, value: 2 },
      { date: '2026-01-02', quantity: 0, value: 0 }
    ]);

    expect(summarizeConsumption(data, 'daily')).toEqual({
      points: data.points,
      averageQuantity: 1.25,
      averageValue: 2.75
    });
  });

  it('groups Monday-Sunday weeks across a year and includes zero and partial weeks in the mean', () => {
    const data = consumption([
      { date: '2025-12-28', quantity: 2, value: 3 },
      { date: '2025-12-29', quantity: 4, value: 6 },
      { date: '2026-01-04', quantity: 6, value: 9 },
      { date: '2026-01-05', quantity: 0, value: 0 },
      { date: '2026-01-12', quantity: 0, value: 0 }
    ]);

    expect(summarizeConsumption(data, 'weekly')).toEqual({
      points: [
        { date: '2025-12-22', quantity: 2, value: 3 },
        { date: '2025-12-29', quantity: 10, value: 15 },
        { date: '2026-01-05', quantity: 0, value: 0 },
        { date: '2026-01-12', quantity: 0, value: 0 }
      ],
      averageQuantity: 3,
      averageValue: 4.5
    });
  });

  it('groups calendar months of different lengths and includes empty months', () => {
    const data = consumption([
      { date: '2026-01-31', quantity: 2, value: 1 },
      { date: '2026-02-01', quantity: 0, value: 0 },
      { date: '2026-03-01', quantity: 1, value: 2 },
      { date: '2026-04-30', quantity: 0, value: 0 }
    ]);

    expect(summarizeConsumption(data, 'monthly')).toEqual({
      points: [
        { date: '2026-01-01', quantity: 2, value: 1 },
        { date: '2026-02-01', quantity: 0, value: 0 },
        { date: '2026-03-01', quantity: 1, value: 2 },
        { date: '2026-04-01', quantity: 0, value: 0 }
      ],
      averageQuantity: 0.75,
      averageValue: 0.75
    });
  });

  it('rounds midpoints like the existing .NET average and handles empty data', () => {
    const data = consumption([
      { date: '2026-01-01', quantity: 17, value: 17 },
      { date: '2026-02-01', quantity: 0, value: 0 },
      { date: '2026-03-01', quantity: 0, value: 0 },
      { date: '2026-04-01', quantity: 0, value: 0 },
      { date: '2026-05-01', quantity: 0, value: 0 },
      { date: '2026-06-01', quantity: 0, value: 0 },
      { date: '2026-07-01', quantity: 0, value: 0 },
      { date: '2026-08-01', quantity: 0, value: 0 }
    ]);

    expect(summarizeConsumption(data, 'monthly').averageQuantity).toBe(2.12);
    expect(summarizeConsumption(data, 'monthly').averageValue).toBe(2.12);
    expect(summarizeConsumption(consumption([]), 'monthly')).toEqual({
      points: [],
      averageQuantity: 0,
      averageValue: 0
    });
  });

  it('keeps local calendar dates in the correct weeks across the daylight saving change', () => {
    const data = consumption([
      { date: '2026-03-29', quantity: 3, value: 3 },
      { date: '2026-03-30', quantity: 5, value: 5 }
    ]);

    expect(summarizeConsumption(data, 'weekly').points).toEqual([
      { date: '2026-03-23', quantity: 3, value: 3 },
      { date: '2026-03-30', quantity: 5, value: 5 }
    ]);
  });
});
