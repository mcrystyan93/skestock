import { TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { type FruitDessertPlanEntryDto } from '@ske/models';
import { CongregationsHttp } from '@ske/shared/congregations';
import { FruitDessertListSmall } from './fruit-dessert-list-small';

describe('FruitDessertListSmall', () => {
  const entry = (id: string, date: string): FruitDessertPlanEntryDto => ({
    id,
    date,
    congregationId: null,
    congregationName: null,
    congregationDeleted: false,
    contactPerson: null,
    contactPhone: null,
    normalizedPhone: null,
    emailSent: false,
    reminderSent: false,
    thanksSent: false
  });

  function create(entries: FruitDessertPlanEntryDto[], loading = false) {
    TestBed.configureTestingModule({
      providers: [
        { provide: CongregationsHttp, useValue: { getAll: vi.fn(), getById: vi.fn() } },
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:circle-check', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:paper-plane', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:file-pdf', icon: '<svg viewBox="0 0 24 24"></svg>' }
        ])
      ]
    });
    const fixture = TestBed.createComponent(FruitDessertListSmall);
    fixture.componentRef.setInput('entries', entries);
    fixture.componentRef.setInput('loading', loading);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders one card per day', () => {
    const el = create([entry('a', '2026-11-09'), entry('b', '2026-11-10')]);

    expect(el.querySelectorAll('ske-fruit-dessert-card')).toHaveLength(2);
  });

  it('shows the empty state when there are no days', () => {
    expect(create([]).textContent).toContain('Clasa nu are zile în interval');
  });

  it('shows skeletons instead of the empty state while loading', () => {
    const el = create([], true);

    expect(el.querySelector('[aria-busy="true"]')).not.toBeNull();
    expect(el.textContent).not.toContain('Clasa nu are zile în interval');
  });
});
