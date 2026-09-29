import { TestBed } from '@angular/core/testing';
import { provideDispatcher } from '@ngrx/signals/events';
import { type CreateOrderListRequest, type OrderListDto, type UpdateOrderListRequest } from '@ske/models';
import { of, throwError } from 'rxjs';
import { OrderListDetailState, NEW_ORDER_LIST_ROUTE_ID } from './order-list-detail.store';
import { OrderListsHttp } from './order-lists.http';

describe('OrderListDetailState', () => {
  let orderListsHttp: {
    getById: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    orderListsHttp = {
      getById: vi.fn(),
      create: vi.fn(),
      update: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        provideDispatcher(),
        OrderListDetailState,
        { provide: OrderListsHttp, useValue: orderListsHttp }
      ]
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
