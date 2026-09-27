import { ClassDailyConsumptionDto, DailyConsumptionPointDto } from '@ske/models';

export type ConsumptionPeriod = 'daily' | 'weekly' | 'monthly';

type ConsumptionSummary = Pick<
  ClassDailyConsumptionDto,
  'points' | 'averageQuantity' | 'averageValue'
>;

function periodStart(date: string, period: Exclude<ConsumptionPeriod, 'daily'>): string {
  if (period === 'monthly') return `${date.slice(0, 7)}-01`;

  const monday = new Date(`${date}T00:00:00Z`);
  monday.setUTCDate(monday.getUTCDate() - ((monday.getUTCDay() + 6) % 7));
  return monday.toISOString().slice(0, 10);
}

function roundAverage(value: number): number {
  const scaled = value * 100;
  const integer = Math.floor(scaled);
  // Match .NET's midpoint-to-even rounding used by the daily API.
  if (Math.abs(scaled - integer - 0.5) < 1e-9) {
    return (integer + (integer % 2)) / 100;
  }
  return Math.round(scaled) / 100;
}

export function summarizeConsumption(
  data: ClassDailyConsumptionDto,
  period: ConsumptionPeriod
): ConsumptionSummary {
  if (period === 'daily') {
    return {
      points: data.points,
      averageQuantity: data.averageQuantity,
      averageValue: data.averageValue
    };
  }

  const grouped = new Map<string, DailyConsumptionPointDto>();
  for (const point of data.points) {
    const date = periodStart(point.date, period);
    const current = grouped.get(date);
    grouped.set(date, {
      date,
      quantity: (current?.quantity ?? 0) + point.quantity,
      value: (current?.value ?? 0) + point.value
    });
  }

  const points = [...grouped.values()];
  return {
    points,
    averageQuantity: points.length ? roundAverage(data.totalQuantity / points.length) : 0,
    averageValue: points.length ? roundAverage(data.totalValue / points.length) : 0
  };
}
