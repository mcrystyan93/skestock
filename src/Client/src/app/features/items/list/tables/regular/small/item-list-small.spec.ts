import { TestBed } from '@angular/core/testing';
import { ItemListSmall } from './item-list-small';

describe('ItemListSmall', () => {
  const filter = { filters: [], pageSize: 50, sort: [] };

  function createFixture() {
    const fixture = TestBed.createComponent(ItemListSmall);
    fixture.componentRef.setInput('items', []);
    fixture.componentRef.setInput('filter', filter);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ItemListSmall],
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
});

function createScrollEvent(scrollHeight: number, scrollTop: number, clientHeight: number): Event {
  const element = { scrollHeight, scrollTop, clientHeight } as HTMLElement;
  const event = new Event('scroll');
  Object.defineProperty(event, 'currentTarget', { value: element });
  return event;
}
