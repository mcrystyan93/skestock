import { TestBed } from '@angular/core/testing';
import { GetAllStockBatchesRequest } from '@ske/models';
import { StockBatchesListSmall } from './stock-batches-list-small';

describe('StockBatchesListSmall', () => {
  const filter: GetAllStockBatchesRequest = {
    filters: [],
    cursor: null,
    pageSize: 50,
    sort: []
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [StockBatchesListSmall]
    });
  });

  it('shows skeleton rows while the receipt batches load', () => {
    const fixture = createFixture([], true);
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelectorAll('nz-skeleton').length).toBe(3);
    expect(element.querySelector('nz-empty')).toBeNull();
    fixture.destroy();
  });

  it('shows an empty state when a receipt has no batches', () => {
    const fixture = createFixture();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nu există articole în această recepție');
    fixture.destroy();
  });

  it('emits load more when scrolling near the bottom', () => {
    const fixture = createFixture([]);
    const emit = vi.spyOn(fixture.componentInstance.onLoadMore, 'emit');

    fixture.componentInstance.onScroll(createScrollEvent(1000, 700, 100));

    expect(emit).toHaveBeenCalledOnce();
    fixture.destroy();
  });

  function createFixture(items: never[] = [], loading = false) {
    const fixture = TestBed.createComponent(StockBatchesListSmall);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('filter', filter);
    fixture.componentRef.setInput('loading', loading);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }
});

function createScrollEvent(scrollHeight: number, scrollTop: number, clientHeight: number): Event {
  const element = { scrollHeight, scrollTop, clientHeight } as HTMLElement;
  const event = new Event('scroll');
  Object.defineProperty(event, 'currentTarget', { value: element });
  return event;
}
