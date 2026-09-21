import { TestBed } from '@angular/core/testing';
import { type OrderListDto } from '@ske/models';
import { NzMessageService } from 'ng-zorro-antd/message';
import { of, throwError } from 'rxjs';
import { OrderListListStore } from './order-list-list.store';
import { OrderListsHttp } from './order-lists.http';

describe('OrderListListStore', () => {
  let orderListsHttp: {
    getAll: ReturnType<typeof vi.fn>;
    submit: ReturnType<typeof vi.fn>;
    cancel: ReturnType<typeof vi.fn>;
    reopen: ReturnType<typeof vi.fn>;
  };
  let messageService: {
    success: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    orderListsHttp = {
      getAll: vi.fn(),
      submit: vi.fn(),
      cancel: vi.fn(),
      reopen: vi.fn()
    };
    messageService = {
      success: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        OrderListListStore,
        {provide: OrderListsHttp, useValue: orderListsHttp},
        {provide: NzMessageService, useValue: messageService}
      ]
    });
  });

  it('reopens a cancelled order list and reloads the current collection', () => {
    const orderList = createOrderList('Draft', null);
    orderListsHttp.reopen.mockReturnValue(of(orderList));
    orderListsHttp.getAll.mockReturnValue(of({
      data: [],
      hasNextPage: false,
      nextCursor: null,
      sort: []
    }));

    const store = TestBed.inject(OrderListListStore);

    store.changeStatus({id: orderList.id, action: 'reopen'});

    expect(orderListsHttp.reopen).toHaveBeenCalledWith(orderList.id);
    expect(orderListsHttp.getAll).toHaveBeenCalled();
    expect(store.statusChangingId()).toBeNull();
    expect(messageService.success).toHaveBeenCalledWith(
      'Comanda a fost redeschisă ca ciornă.'
    );
  });

  it('exposes status-change failures and clears the pending row', () => {
    orderListsHttp.cancel.mockReturnValue(throwError(() => ({
      status: 409,
      title: 'Order list cannot be cancelled'
    })));

    const store = TestBed.inject(OrderListListStore);

    store.changeStatus({id: 'order-list-1', action: 'cancel'});

    expect(store.statusChangingId()).toBeNull();
    expect(store.orderListsProblemDetail()).toEqual({
      status: 409,
      title: 'Order list cannot be cancelled'
    });
    expect(messageService.success).not.toHaveBeenCalled();
  });
});

function createOrderList(status: OrderListDto['status'], submittedAt: string | null): OrderListDto {
  return {
    id: 'order-list-1',
    classId: 'class-1',
    className: 'Clasa I',
    name: 'Listă',
    note: null,
    status,
    submittedAt,
    lines: [],
    createdByName: null,
    lastModifiedByName: null,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };
}
