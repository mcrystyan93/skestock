import { TestBed } from '@angular/core/testing';
import { ExpenseDto } from '@ske/models';
import { ExpenseListSmall } from './expense-list-small';

describe('ExpenseListSmall', () => {
  function createFixture(items: ExpenseDto[] = [], loading = false) {
    const fixture = TestBed.createComponent(ExpenseListSmall);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('filter', { filters: [], pageSize: 50, sort: [] });
    fixture.componentRef.setInput('loading', loading);
    fixture.componentRef.setInput('hasNextPage', false);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => TestBed.configureTestingModule({ imports: [ExpenseListSmall] }));

  it('shows skeleton cards while the first page is loading', () => {
    const fixture = createFixture([], true);

    expect(fixture.nativeElement.querySelectorAll('nz-skeleton').length).toBe(4);
  });

  it('shows the empty state when there are no expenses', () => {
    const fixture = createFixture();

    expect(fixture.nativeElement.querySelector('nz-empty')).not.toBeNull();
  });
});
