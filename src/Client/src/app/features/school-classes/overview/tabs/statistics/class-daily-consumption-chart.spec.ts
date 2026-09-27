import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ClassDailyConsumptionDto } from '@ske/models';
import { ThemeService } from '@ske/theme';
import { ClassDailyConsumptionChart } from './class-daily-consumption-chart';

const data: ClassDailyConsumptionDto = {
  classId: 'class-id',
  fromDate: '2025-12-29',
  toDate: '2026-01-05',
  points: [
    { date: '2025-12-29', quantity: 2, value: 5 },
    { date: '2025-12-30', quantity: 0, value: 0 },
    { date: '2026-01-05', quantity: 6, value: 9 }
  ],
  totalQuantity: 8,
  totalValue: 14,
  averageQuantity: 1,
  averageValue: 1.75
};

describe('ClassDailyConsumptionChart', () => {
  it('switches bar totals, period averages and accessible description without changing daily data', async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: ThemeService, useValue: { currentTheme: signal('light') } }]
    });
    TestBed.overrideComponent(ClassDailyConsumptionChart, {
      set: { template: '', imports: [] }
    });

    const fixture = TestBed.createComponent(ClassDailyConsumptionChart);
    fixture.componentRef.setInput('data', data);
    fixture.componentRef.setInput('emptyMessage', 'Nu exista consum');
    fixture.componentRef.setInput('period', 'weekly');
    await fixture.whenStable();

    const chart = fixture.componentInstance;
    expect(chart.series()[0].data).toEqual([
      { x: Date.parse('2025-12-29T00:00:00Z'), y: 2 },
      { x: Date.parse('2026-01-05T00:00:00Z'), y: 6 }
    ]);
    expect(chart.averageQuantityLabel()).toBe('4');
    expect(chart.averageValueLabel()).toContain('7');
    expect(chart.ariaLabel()).toContain('săptămânal');
    expect(chart.annotations()?.yaxis?.[0].y).toBe(4);
    const weekLabel = chart.tooltip()?.x?.formatter?.(Date.parse('2025-12-29T00:00:00Z'));
    expect(weekLabel).toContain('2025');
    expect(weekLabel).toContain('2026');

    fixture.componentRef.setInput('period', 'monthly');
    await fixture.whenStable();
    expect(chart.series()[0].data).toEqual([
      { x: Date.parse('2025-12-01T00:00:00Z'), y: 2 },
      { x: Date.parse('2026-01-01T00:00:00Z'), y: 6 }
    ]);
    expect(chart.tooltip()?.x?.formatter?.(Date.parse('2026-01-01T00:00:00Z'))).toContain('ianuarie 2026');
    expect(chart.ariaLabel()).toContain('lunar');

    fixture.componentRef.setInput('period', 'daily');
    await fixture.whenStable();

    expect(chart.series()[0].data).toHaveLength(3);
    expect(chart.averageQuantityLabel()).toBe('1');
    expect(chart.averageValueLabel()).toContain('1');
    expect(data.points).toHaveLength(3);
  });
});
