import { TestBed } from '@angular/core/testing';
import { SchoolClassListSmall } from './school-class-list-small';

describe('SchoolClassListSmall', () => {
  const filter = { filters: [], pageSize: 50, sort: [] };

  function createFixture() {
    const fixture = TestBed.createComponent(SchoolClassListSmall);
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
      imports: [SchoolClassListSmall],
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
