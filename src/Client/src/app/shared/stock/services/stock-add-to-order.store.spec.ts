import { TestBed } from '@angular/core/testing';
import { Events, provideDispatcher } from '@ngrx/signals/events';
import { type OrderListDto, type OrderListListItemDto } from '@ske/models';
import { OrderListsHttp } from '@ske/shared/order-lists';
import { of, throwError } from 'rxjs';
import { StockAddToOrderState, stockAddToOrderApiEvents } from './stock-add-to-order.store';

describe('StockAddToOrderState', () => {
  let http: { getAll: ReturnType<typeof vi.fn>; addItem: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    http = { getAll: vi.fn(), addItem: vi.fn() };

    TestBed.configureTestingModule({
      providers: [provideDispatcher(), StockAddToOrderState, { provide: OrderListsHttp, useValue: http }]
    });
  });

  it('loads only the draft order lists of the class', () => {
    const draft = { id: 'o-1', name: 'Ciornă' } as OrderListListItemDto;
    http.getAll.mockReturnValue(of({ data: [draft], hasNextPage: false }));

    const store = TestBed.inject(StockAddToOrderState);
    store.loadDrafts('class-1');

    expect(store.drafts()).toEqual([draft]);
    expect(http.getAll.mock.calls[0][0].filters).toEqual([
      { field: 'classId', operator: 'equals', fieldType: 'number', value: 'class-1' },
      { field: 'status', operator: 'equals', fieldType: 'string', value: 'Draft' }
    ]);
  });

  it('dispatches addSuccess with the resulting order list', () => {
    const orderList = { id: 'o-1', name: 'Ciornă' } as OrderListDto;
    http.addItem.mockReturnValue(of(orderList));

    const received: OrderListDto[] = [];
    TestBed.inject(Events).on(stockAddToOrderApiEvents.addSuccess).subscribe(({ payload }) => received.push(payload));

    const store = TestBed.inject(StockAddToOrderState);
    store.addItem({ classId: 'c', itemId: 'i', quantity: 2, orderListId: 'o-1' });

    expect(received).toEqual([orderList]);
    expect(store.addToOrderLoading()).toBe(false);
  });

  it('exposes API errors and stops loading when adding fails', () => {
    http.addItem.mockReturnValue(throwError(() => ({ status: 409, title: 'Conflict' })));

    const store = TestBed.inject(StockAddToOrderState);
    store.addItem({ classId: 'c', itemId: 'i', quantity: 1, newOrderListName: 'Nou' });

    expect(store.addToOrderProblemDetail()).toEqual({ status: 409, title: 'Conflict' });
    expect(store.addToOrderLoading()).toBe(false);
  });
});
