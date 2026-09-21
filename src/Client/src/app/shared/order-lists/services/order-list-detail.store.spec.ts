import { TestBed } from '@angular/core/testing';
import { provideDispatcher } from '@ngrx/signals/events';
import { type CreateOrderListRequest, type LowStockItemDto, type OrderListDto, type UpdateOrderListRequest } from '@ske/models';
import { of, throwError } from 'rxjs';
import { StockHttp } from '@ske/shared/stock';
import { OrderListDetailState, NEW_ORDER_LIST_ROUTE_ID } from './order-list-detail.store';
import { OrderListsHttp } from './order-lists.http';

describe('OrderListDetailState', () => {
  let orderListsHttp: {
    getById: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
  };
  let stockHttp: {
    getLowStockItems: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    orderListsHttp = {
      getById: vi.fn(),
      create: vi.fn(),
      update: vi.fn()
    };
    stockHttp = {
      getLowStockItems: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        provideDispatcher(),
        OrderListDetailState,
        { provide: OrderListsHttp, useValue: orderListsHttp },
        { provide: StockHttp, useValue: stockHttp }
      ]
    });
  });

  it('groups and sorts successful low-stock results', () => {
    stockHttp.getLowStockItems.mockReturnValue(of([
      createLowStockItem('Zonă B', 'item-2'),
      createLowStockItem('Zonă A', 'item-1')
    ]));

    const store = TestBed.inject(OrderListDetailState);
    store.loadLowStockItems('class-1');

    expect(stockHttp.getLowStockItems).toHaveBeenCalledWith('class-1');
    expect(store.lowStockItemsLoading()).toBe(false);
    expect(store.lowStockItemsLoaded()).toBe(true);
    expect([...store.lowStockItemsByLocation().keys()]).toEqual(['Zonă A', 'Zonă B']);
  });

  it('retains a visible error state when low-stock loading fails', () => {
    stockHttp.getLowStockItems.mockReturnValue(throwError(() => ({
      status: 503,
      title: 'Stock unavailable'
    })));

    const store = TestBed.inject(OrderListDetailState);
    store.loadLowStockItems('class-1');

    expect(store.lowStockItemsLoading()).toBe(false);
    expect(store.lowStockItemsLoaded()).toBe(true);
    expect(store.lowStockItemsProblemDetail()).toEqual({
      status: 503,
      title: 'Stock unavailable'
    });
  });

  it('rejects an empty class id before calling the low-stock API', () => {
    const store = TestBed.inject(OrderListDetailState);
    store.loadLowStockItems('');

    expect(stockHttp.getLowStockItems).not.toHaveBeenCalled();
    expect(store.lowStockItemsLoaded()).toBe(true);
    expect(store.lowStockItemsProblemDetail()).toEqual({
      status: 400,
      title: 'Class ID missing'
    });
  });

  it('rejects a create request with a missing class id', () => {
    const store = TestBed.inject(OrderListDetailState);
    store.loadOrderList({ id: NEW_ORDER_LIST_ROUTE_ID });

    const request: CreateOrderListRequest = {
      classId: '',
      name: 'Listă',
      note: null,
      lines: []
    };

    expect(store.saveOrderList(request, 'save-1')).toBe(false);
    expect(orderListsHttp.create).not.toHaveBeenCalled();
    expect(store.orderListProblemDetail()).toEqual({
      status: 400,
      title: 'Class ID missing'
    });
  });

  it('clears the save loading state and exposes API errors after update failure', () => {
    const existingOrderList = createOrderList();
    orderListsHttp.getById.mockReturnValue(of(existingOrderList));
    orderListsHttp.update.mockReturnValue(throwError(() => ({
      status: 400,
      title: 'Invalid order list'
    })));

    const store = TestBed.inject(OrderListDetailState);
    store.loadOrderList({ id: existingOrderList.id });

    const request: UpdateOrderListRequest = {
      name: 'Listă actualizată',
      note: null,
      lines: []
    };

    expect(store.saveOrderList(request, 'save-2')).toBe(true);
    expect(orderListsHttp.update).toHaveBeenCalledWith(existingOrderList.id, request);
    expect(store.orderListLoading()).toBe(false);
    expect(store.orderListProblemDetail()).toEqual({
      status: 400,
      title: 'Invalid order list'
    });
  });
});

function createLowStockItem(locationName: string, itemId: string): LowStockItemDto {
  return {
    itemId,
    itemName: `Articol ${itemId}`,
    sku: null,
    unit: 'buc',
    locationName
  };
}

function createOrderList(): OrderListDto {
  return {
    id: 'order-list-1',
    classId: 'class-1',
    className: 'Clasa I',
    name: 'Listă',
    note: null,
    status: 'Draft',
    submittedAt: null,
    lines: [],
    createdByName: null,
    lastModifiedByName: null,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };
}
