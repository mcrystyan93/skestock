import { TestBed } from '@angular/core/testing';
import { ExpenseTable } from './expense-table';

describe('ExpenseTable', () => {
  beforeEach(() => {
    // jsdom has no ResizeObserver, which the base table uses to size the virtual scroll area.
    vi.stubGlobal('ResizeObserver', class {
      observe() {}
      unobserve() {}
      disconnect() {}
    });
    TestBed.configureTestingModule({ imports: [ExpenseTable] });
  });

  it('renders the column headers', () => {
    const fixture = TestBed.createComponent(ExpenseTable);
    fixture.componentRef.setInput('items', []);
    fixture.componentRef.setInput('filter', { filters: [], pageSize: 50, sort: [] });
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('hasNextPage', false);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Furnizor');
  });
});
