import { TestBed } from '@angular/core/testing';
import { CategoryDto } from '@ske/models';
import { CategoryListSmall } from './category-list-small';

describe('CategoryListSmall', () => {
  const filter = { filters: [], pageSize: 50, sort: [] };

  function createFixture(loading = false) {
    const fixture = TestBed.createComponent(CategoryListSmall);
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
      imports: [CategoryListSmall]
    });
  });

  it('emits onLoadMore when scrolling within the bottom threshold', () => {
    const fixture = createFixture();
    const emit = vi.spyOn(fixture.componentInstance.onLoadMore, 'emit');

    fixture.componentInstance.onScroll(createScrollEvent(1000, 700, 100));

    expect(emit).toHaveBeenCalledOnce();
    fixture.destroy();
  });

  it('shows skeleton cards while loading', () => {
    const fixture = createFixture(true);

    expect((fixture.nativeElement as HTMLElement).querySelectorAll('nz-skeleton').length).toBe(4);
    fixture.destroy();
  });

  it('shows the empty state when there are no categories', () => {
    const fixture = createFixture();

    expect((fixture.nativeElement as HTMLElement).querySelector('nz-empty')).not.toBeNull();
    fixture.destroy();
  });

  it('emits onEdit on space without scrolling the page', () => {
    const fixture = createFixture();
    const emit = vi.spyOn(fixture.componentInstance.onEdit, 'emit');
    const event = new KeyboardEvent('keydown', { key: ' ', cancelable: true });
    const category = { id: 'c1', name: 'Papetărie' } as CategoryDto;

    (fixture.componentInstance as unknown as { onSpace(e: Event, c: CategoryDto): void }).onSpace(event, category);

    expect(event.defaultPrevented).toBe(true);
    expect(emit).toHaveBeenCalledWith(category);
    fixture.destroy();
  });

  it('does not emit when another page cannot be loaded', () => {
    const fixture = createFixture();
    const component = fixture.componentInstance;
    const emit = vi.spyOn(component.onLoadMore, 'emit');
    fixture.componentRef.setInput('hasNextPage', false);
    fixture.detectChanges();

    component.onScroll(createScrollEvent(900, 700, 100));

    expect(emit).not.toHaveBeenCalled();
    fixture.destroy();
  });

  it('does not emit while another page is loading', () => {
    const fixture = createFixture();
    const component = fixture.componentInstance;
    const emit = vi.spyOn(component.onLoadMore, 'emit');
    fixture.componentRef.setInput('isLoadingMore', true);
    fixture.detectChanges();

    component.onScroll(createScrollEvent(900, 700, 100));

    expect(emit).not.toHaveBeenCalled();
    fixture.destroy();
  });
});

function createScrollEvent(scrollHeight: number, scrollTop: number, clientHeight: number): Event {
  const element = { scrollHeight, scrollTop, clientHeight } as HTMLElement;
  const event = new Event('scroll');
  Object.defineProperty(event, 'currentTarget', { value: element });
  return event;
}
