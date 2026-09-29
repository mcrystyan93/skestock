import { TestBed } from '@angular/core/testing';
import { SupplyListListItemDto } from '@ske/models';
import { SupplyListSmall } from './supply-list-small';

describe('SupplyListSmall', () => {
  const filter = { filters: [], pageSize: 50, sort: [] };

  function createFixture(items: SupplyListListItemDto[] = [], loading = false) {
    const fixture = TestBed.createComponent(SupplyListSmall);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('filter', filter);
    fixture.componentRef.setInput('loading', loading);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [SupplyListSmall] });
  });

  it('emits onLoadMore when scrolling within the bottom threshold', () => {
    const fixture = createFixture();
    const emit = vi.spyOn(fixture.componentInstance.onLoadMore, 'emit');

    fixture.componentInstance.onScroll(createScrollEvent(1000, 700, 100));

    expect(emit).toHaveBeenCalledOnce();
    fixture.destroy();
  });

  it('shows skeleton cards while the first page is loading', () => {
    const fixture = createFixture([], true);
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelectorAll('nz-skeleton').length).toBe(4);
    expect(element.querySelector('nz-empty')).toBeNull();
    fixture.destroy();
  });

  it('shows the empty state when there are no lists', () => {
    const fixture = createFixture();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('nz-empty')).not.toBeNull();
    expect(element.querySelector('cdk-virtual-scroll-viewport')).toBeNull();
    fixture.destroy();
  });
});

function createScrollEvent(scrollHeight: number, scrollTop: number, clientHeight: number): Event {
  const element = { scrollHeight, scrollTop, clientHeight } as HTMLElement;
  const event = new Event('scroll');
  Object.defineProperty(event, 'currentTarget', { value: element });
  return event;
}
