import { TestBed } from '@angular/core/testing';
import { ClassStatisticsStore } from '../../../services/class-statistics.store';
import { DailyConsumptionCard } from './daily-consumption-card';

describe('DailyConsumptionCard', () => {
  it('defaults to daily and switches periods without reloading or resetting filters', async () => {
    const loadDailyConsumption = vi.fn();
    TestBed.configureTestingModule({
      providers: [{ provide: ClassStatisticsStore, useValue: { loadDailyConsumption } }]
    });
    TestBed.overrideComponent(DailyConsumptionCard, { set: { template: '', imports: [] } });

    const fixture = TestBed.createComponent(DailyConsumptionCard);
    fixture.componentRef.setInput('classId', 'class-id');
    fixture.componentRef.setInput('active', true);
    await fixture.whenStable();

    const card = fixture.componentInstance;
    expect(card.filterForm.period().value()).toBe('daily');
    expect(card.title()).toBe('Consum mediu zilnic');

    card.filterForm.location().value.set({ id: 'location-id', name: 'Depozit' });
    await fixture.whenStable();
    loadDailyConsumption.mockClear();

    card.filterForm.period().value.set('weekly');
    await fixture.whenStable();
    expect(card.title()).toBe('Consum mediu săptămânal');
    expect(card.filterForm.location().value()?.id).toBe('location-id');
    expect(loadDailyConsumption).not.toHaveBeenCalled();

    card.filterForm.period().value.set('monthly');
    await fixture.whenStable();
    expect(card.title()).toBe('Consum mediu lunar');
    expect(loadDailyConsumption).not.toHaveBeenCalled();
  });
});
