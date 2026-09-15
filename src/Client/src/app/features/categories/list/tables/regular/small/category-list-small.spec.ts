import { TestBed } from '@angular/core/testing';
import { CategoryListSmall } from './category-list-small';

describe('CategoryListSmall', () => {
  const filter = { filters: [], pageSize: 50, sort: [] };

  function createFixture() {
    const fixture = TestBed.createComponent(CategoryListSmall);
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
      imports: [CategoryListSmall]
    });
  });

  it('emits onLoadMore when scrolling within the bottom threshold', () => {
    const fixture = createFixture();
    const component = fixture.componentInstance;
    const emit = vi.spyOn(component.onLoadMore, 'emit');
    const viewport = fixture.nativeElement.querySelector('cdk-virtual-scroll-viewport') as HTMLElement;

    Object.defineProperties(viewport, {
      scrollHeight: { value: 1000, configurable: true },
      scrollTop: { value: 700, configurable: true },
      clientHeight: { value: 100, configurable: true }
    });

    viewport.dispatchEvent(new Event('scroll'));

    expect(emit).toHaveBeenCalledOnce();
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
