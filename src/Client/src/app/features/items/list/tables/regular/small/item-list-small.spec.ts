import { TestBed } from '@angular/core/testing';
import { ItemDto } from '@ske/models';
import { ItemListSmall } from './item-list-small';

describe('ItemListSmall', () => {
  const filter = { filters: [], pageSize: 50, sort: [] };

  function createFixture(items: ItemDto[] = [], loading = false) {
    const fixture = TestBed.createComponent(ItemListSmall);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('filter', filter);
    fixture.componentRef.setInput('loading', loading);
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

  it('shows skeleton cards while the first page is loading', () => {
    const fixture = createFixture([], true);
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelectorAll('nz-skeleton').length).toBe(4);
    expect(element.querySelector('nz-empty')).toBeNull();
    fixture.destroy();
  });

  it('shows the empty state when there are no items', () => {
    const fixture = createFixture();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('nz-empty')).not.toBeNull();
    expect(element.querySelector('cdk-virtual-scroll-viewport')).toBeNull();
    fixture.destroy();
  });

  it('builds the subtitle from sku and category', () => {
    const fixture = createFixture();
    const component = fixture.componentInstance as unknown as { subtitle(item: ItemDto): string };

    expect(component.subtitle(createItem({ sku: 'SKU-1', categoryName: 'Papetărie' }))).toBe('SKU-1 · Papetărie');
    expect(component.subtitle(createItem({ sku: null, categoryName: null }))).toBe('—');
    fixture.destroy();
  });

  it('emits onEdit on space without scrolling the page', () => {
    const fixture = createFixture();
    const component = fixture.componentInstance;
    const emit = vi.spyOn(component.onEdit, 'emit');
    const item = createItem();
    const event = new KeyboardEvent('keydown', { key: ' ', cancelable: true });

    (component as unknown as { onSpace(event: Event, item: ItemDto): void }).onSpace(event, item);

    expect(event.defaultPrevented).toBe(true);
    expect(emit).toHaveBeenCalledWith(item);
    fixture.destroy();
  });
});

function createItem(overrides: Partial<ItemDto> = {}): ItemDto {
  return {
    id: 'item-1',
    sku: 'SKU-1',
    name: 'Caiet dictando',
    unit: 'buc',
    minThreshold: 5,
    isPerishable: false,
    isActive: true,
    categoryId: 'category-1',
    categoryName: 'Papetărie',
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

function createScrollEvent(scrollHeight: number, scrollTop: number, clientHeight: number): Event {
  const element = { scrollHeight, scrollTop, clientHeight } as HTMLElement;
  const event = new Event('scroll');
  Object.defineProperty(event, 'currentTarget', { value: element });
  return event;
}
