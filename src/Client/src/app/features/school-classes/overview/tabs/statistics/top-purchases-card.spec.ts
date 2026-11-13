import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ClassStatisticsStore } from '../../../services/class-statistics.store';
import { TopPurchasesCard } from './top-purchases-card';

describe('TopPurchasesCard', () => {
  function setup(kind?: 'donations') {
    const store = {
      loadTopPurchases: vi.fn(),
      loadTopDonations: vi.fn(),
      topPurchases: signal({ computedAt: '2026-09-01T00:00:00Z', byQuantity: [], byValue: [], byFrequency: [] }),
      topDonations: signal({ computedAt: '2026-09-02T00:00:00Z', byQuantity: [], byValue: [], byFrequency: [] }),
      topPurchasesLoading: signal(false),
      topDonationsLoading: signal(true),
      topPurchasesProblemDetail: signal(null),
      topDonationsProblemDetail: signal(null),
      topPurchasesValidationErrors: signal(null),
      topDonationsValidationErrors: signal(null)
    };
    TestBed.configureTestingModule({ providers: [{ provide: ClassStatisticsStore, useValue: store }] });
    TestBed.overrideComponent(TopPurchasesCard, { set: { template: '', imports: [] } });
    const fixture = TestBed.createComponent(TopPurchasesCard);
    fixture.componentRef.setInput('classId', 'class-id');
    if (kind) fixture.componentRef.setInput('kind', kind);
    return { fixture, store };
  }

  it('does not load while inactive', async () => {
    const { fixture, store } = setup();
    await fixture.whenStable();

    expect(store.loadTopPurchases).not.toHaveBeenCalled();
    expect(store.loadTopDonations).not.toHaveBeenCalled();
  });

  it('loads purchases by default and shows when they were computed', async () => {
    const { fixture, store } = setup();
    fixture.componentRef.setInput('active', true);
    await fixture.whenStable();

    expect(store.loadTopPurchases).toHaveBeenLastCalledWith({ scope: 'Class', classId: 'class-id', categoryId: null });
    expect(store.loadTopDonations).not.toHaveBeenCalled();
    expect(fixture.componentInstance.text().title).toBe('Top achiziții');
    expect(fixture.componentInstance.computedAtLabel()).not.toBeNull();
  });

  it('loads donations, reloads on scope change and hides the computed-at label', async () => {
    const { fixture, store } = setup('donations');
    fixture.componentRef.setInput('active', true);
    await fixture.whenStable();

    expect(store.loadTopDonations).toHaveBeenLastCalledWith({ scope: 'Class', classId: 'class-id', categoryId: null });
    expect(store.loadTopPurchases).not.toHaveBeenCalled();
    expect(fixture.componentInstance.text().title).toBe('Top donații');
    expect(fixture.componentInstance.loading()).toBe(true);
    expect(fixture.componentInstance.computedAtLabel()).toBeNull();

    fixture.componentInstance.filterForm.scope().value.set('Last90Days');
    await fixture.whenStable();

    expect(store.loadTopDonations).toHaveBeenLastCalledWith({ scope: 'Last90Days', classId: 'class-id', categoryId: null });
  });
});
