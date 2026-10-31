import { TestBed } from '@angular/core/testing';
import { CongregationDto } from '@ske/models';
import { CongregationListSmall } from './congregation-list-small';

describe('CongregationListSmall', () => {
  const filter = { filters: [], pageSize: 50, sort: [] };
  const congregation: CongregationDto = {
    id: 'c1',
    name: 'Central',
    email: 'central@example.com',
    canReceiveStudents: true,
    isSmallCongregation: false,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };

  function createFixture(loading = false, items: CongregationDto[] = []) {
    const fixture = TestBed.createComponent(CongregationListSmall);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('filter', filter);
    fixture.componentRef.setInput('loading', loading);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [CongregationListSmall] });
  });

  it('shows skeleton cards while loading', () => {
    const fixture = createFixture(true);

    expect((fixture.nativeElement as HTMLElement).querySelectorAll('nz-skeleton').length).toBe(4);
    fixture.destroy();
  });

  it('shows the empty state when there are no congregations', () => {
    const fixture = createFixture();

    expect((fixture.nativeElement as HTMLElement).querySelector('nz-empty')).not.toBeNull();
    fixture.destroy();
  });

  it('emits onEdit on space without scrolling the page', () => {
    const fixture = createFixture();
    const emit = vi.spyOn(fixture.componentInstance.onEdit, 'emit');
    const event = new KeyboardEvent('keydown', { key: ' ', cancelable: true });

    (fixture.componentInstance as unknown as { onSpace(e: Event, c: CongregationDto): void }).onSpace(event, congregation);

    expect(event.defaultPrevented).toBe(true);
    expect(emit).toHaveBeenCalledWith(congregation);
    fixture.destroy();
  });

  it('emits onLoadMore when scrolling within the bottom threshold', () => {
    const fixture = createFixture();
    const emit = vi.spyOn(fixture.componentInstance.onLoadMore, 'emit');

    fixture.componentInstance.onScroll(createScrollEvent(1000, 700, 100));

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
