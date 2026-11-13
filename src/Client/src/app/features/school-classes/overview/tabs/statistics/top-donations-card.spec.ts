import { TestBed } from '@angular/core/testing';
import { ClassStatisticsStore } from '../../../services/class-statistics.store';
import { TopDonationsCard } from './top-donations-card';

describe('TopDonationsCard', () => {
  function setup() {
    const loadTopDonations = vi.fn();
    TestBed.configureTestingModule({
      providers: [{ provide: ClassStatisticsStore, useValue: { loadTopDonations } }]
    });
    TestBed.overrideComponent(TopDonationsCard, { set: { template: '', imports: [] } });
    const fixture = TestBed.createComponent(TopDonationsCard);
    fixture.componentRef.setInput('classId', 'class-id');
    return { fixture, loadTopDonations };
  }

  it('does not load while inactive', async () => {
    const { fixture, loadTopDonations } = setup();
    await fixture.whenStable();

    expect(loadTopDonations).not.toHaveBeenCalled();
  });

  it('loads the current class once active and reloads when the scope changes', async () => {
    const { fixture, loadTopDonations } = setup();
    fixture.componentRef.setInput('active', true);
    await fixture.whenStable();

    expect(loadTopDonations).toHaveBeenLastCalledWith({ scope: 'Class', classId: 'class-id', categoryId: null });

    fixture.componentInstance.filterForm.scope().value.set('Last90Days');
    await fixture.whenStable();

    expect(loadTopDonations).toHaveBeenLastCalledWith({ scope: 'Last90Days', classId: 'class-id', categoryId: null });
  });
});
