import { TestBed } from '@angular/core/testing';
import { GetAllOrderListsRequest, OrderListListItemDto } from '@ske/models';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { OrderListListSmall } from './order-list-list-small';

describe('OrderListListSmall', () => {
  const filter: GetAllOrderListsRequest = {
    filters: [],
    cursor: null,
    pageSize: 50,
    sort: [],
    searchTerm: null
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [OrderListListSmall],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:chevron-down', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:download', icon: '<svg viewBox="0 0 24 24"></svg>' }
        ])
      ]
    });
  });

  it('shows skeleton cards while loading the first page', () => {
    const fixture = createFixture([], true);
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelectorAll('nz-skeleton').length).toBe(4);
    expect(element.querySelector('nz-empty')).toBeNull();
    fixture.destroy();
  });

  it('shows the Romanian empty state when no orders match', () => {
    const fixture = createFixture();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nu există comenzi');
    fixture.destroy();
  });

  it('emits open and download actions from a submitted order card', () => {
    const order = createOrder({ status: 'Submitted' });
    const fixture = createFixture([order]);
    const component = fixture.componentInstance;
    const onView = vi.spyOn(component.onView, 'emit');
    const onDownload = vi.spyOn(component.onDownload, 'emit');
    const element = fixture.nativeElement as HTMLElement;

    element.querySelector<HTMLButtonElement>('button[aria-label="Deschide comanda Comandă test"]')!.click();
    element.querySelector<HTMLButtonElement>('button[aria-label="Descarcă Excel pentru comanda Comandă test"]')!.click();

    expect(onView).toHaveBeenCalledWith(order);
    expect(onDownload).toHaveBeenCalledWith(order);
    fixture.destroy();
  });

  it('keeps the same status actions as the desktop table', () => {
    const fixture = createFixture();

    expect(fixture.componentInstance.availableActions('Draft')).toEqual(['submit', 'cancel']);
    expect(fixture.componentInstance.availableActions('Submitted')).toEqual(['cancel']);
    expect(fixture.componentInstance.availableActions('Cancelled')).toEqual(['reopen']);
    fixture.destroy();
  });

  function createFixture(items: OrderListListItemDto[] = [], loading = false) {
    const fixture = TestBed.createComponent(OrderListListSmall);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('filter', filter);
    fixture.componentRef.setInput('loading', loading);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }
});

function createOrder(overrides: Partial<OrderListListItemDto> = {}): OrderListListItemDto {
  return {
    id: 'order-1',
    classId: 'class-1',
    name: 'Comandă test',
    status: 'Draft',
    lineCount: 3,
    submittedAt: null,
    createdByName: 'Administrator',
    createdDate: '2026-09-29T10:00:00Z',
    lastModifiedDate: '2026-09-29T10:00:00Z',
    ...overrides
  };
}
