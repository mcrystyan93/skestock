import { TestBed } from '@angular/core/testing';
import { ItemImportListSmall } from './item-import-list-small';

describe('ItemImportListSmall', () => {
  const filter = { filters: [], pageSize: 50, sort: [] };

  function createFixture(loading = false) {
    const fixture = TestBed.createComponent(ItemImportListSmall);
    fixture.componentRef.setInput('items', []);
    fixture.componentRef.setInput('filter', filter);
    fixture.componentRef.setInput('loading', loading);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ItemImportListSmall],
    });
  });

  it('emits onLoadMore when scrolling within the bottom threshold', () => {
    const fixture = createFixture();
    const component = fixture.componentInstance;
    const emit = vi.spyOn(component.onLoadMore, 'emit');

    component.onScroll(createScrollEvent(1000, 700, 100));

    expect(emit).toHaveBeenCalledOnce();
    fixture.destroy();
  });

  it('shows skeleton cards while loading', () => {
    const fixture = createFixture(true);
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelectorAll('nz-skeleton').length).toBe(4);
    fixture.destroy();
  });

  it('shows the empty state when there are no imports', () => {
    const fixture = createFixture();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('nz-empty')).not.toBeNull();
    fixture.destroy();
  });
});

function createScrollEvent(scrollHeight: number, scrollTop: number, clientHeight: number): Event {
  const element = { scrollHeight, scrollTop, clientHeight } as HTMLElement;
  const event = new Event('scroll');
  Object.defineProperty(event, 'currentTarget', { value: element });
  return event;
}
