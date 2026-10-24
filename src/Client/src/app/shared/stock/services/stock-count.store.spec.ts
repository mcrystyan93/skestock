import { TestBed } from '@angular/core/testing';
import { provideDispatcher } from '@ngrx/signals/events';
import { AdjustmentReason, ErrorCodes, type StockItemDto } from '@ske/models';
import { of, throwError } from 'rxjs';
import { StockCountState } from './stock-count.store';
import { StockHttp } from './stock.http';

const row = (itemName: string, locationId: string, quantity: number, categoryName = 'Cat'): StockItemDto =>
  ({
    itemId: `i-${itemName}`,
    itemName,
    categoryName,
    locationId,
    locationName: `Loc ${locationId}`,
    quantity
  }) as StockItemDto;

describe('StockCountState', () => {
  let http: {
    getClassLocationStock: ReturnType<typeof vi.fn>;
    bulkAdjustStock: ReturnType<typeof vi.fn>;
  };

  const create = () => {
    http.getClassLocationStock.mockReturnValue(
      of({
        items: [
          row('B', 'b', 5),
          row('A', 'b', 3),
          row('Z', 'a', 2, 'Zcat'),
          row('Y', 'a', 4, 'Acat'),
          row('Zero', 'a', 0)
        ],
        hasExpiredItems: false
      })
    );
    const store = TestBed.inject(StockCountState);
    store.load('c-1');
    return store;
  };

  beforeEach(() => {
    http = { getClassLocationStock: vi.fn(), bulkAdjustStock: vi.fn() };
    TestBed.configureTestingModule({
      providers: [provideDispatcher(), StockCountState, { provide: StockHttp, useValue: http }]
    });
  });

  it('keeps only stocked rows and derives the locations', () => {
    const store = create();
    expect(store.rows().length).toBe(4);
    expect(store.locations().map((l) => l.id)).toEqual(['a', 'b']);
    expect(store.reason()).toBe(AdjustmentReason.Adjustment);
  });

  it('requests the visible report, because the API returns only hidden zero-stock rows for includeHidden: true', () => {
    create();
    expect(http.getClassLocationStock).toHaveBeenCalledWith(expect.objectContaining({ includeHidden: false }));
  });

  it('orders rows by location, category, then item', () => {
    const store = create();
    store.setSelectedLocationIds(['a', 'b']);
    expect(store.rowsInScope().map((r) => r.itemName)).toEqual(['Y', 'Z', 'A', 'B']);
  });

  it('walks locations -> items -> summary and back', () => {
    const store = create();
    store.next();
    expect(store.step()).toBe('locations');

    store.setSelectedLocationIds(['b']);
    store.next();
    expect(store.step()).toBe('items');
    expect(store.currentRow()?.itemName).toBe('A');

    store.previous();
    expect(store.step()).toBe('locations');

    store.next();
    store.next();
    expect(store.currentRow()?.itemName).toBe('B');
    store.next();
    expect(store.step()).toBe('summary');

    store.previous();
    expect(store.step()).toBe('items');
    expect(store.currentIndex()).toBe(1);
  });

  it('records only edited values and computes changes and unchanged count', () => {
    const store = create();
    store.setSelectedLocationIds(['b']);
    const [a, b] = store.rowsInScope();

    store.setCount(a, 1);
    store.setCount(b, 5);
    expect(store.changes().map((c) => [c.row.itemName, c.newQuantity, c.delta])).toEqual([['A', 1, -2]]);
    expect(store.unchangedCount()).toBe(1);

    store.setCount(a, 3);
    expect(store.changes()).toEqual([]);
  });

  it('keeps counts when the locations change and ignores them while out of scope', () => {
    const store = create();
    store.setSelectedLocationIds(['a', 'b']);
    store.setCount(store.rowsInScope()[0], 9);
    store.setSelectedLocationIds(['b']);
    expect(store.changes()).toEqual([]);
    store.setSelectedLocationIds(['a']);
    expect(store.changes().length).toBe(1);
  });

  it('goToRow and jumpToSummary change the step', () => {
    const store = create();
    store.setSelectedLocationIds(['a', 'b']);
    store.jumpToSummary();
    expect(store.step()).toBe('summary');
    store.goToRow('i-A:b');
    expect(store.step()).toBe('items');
    expect(store.currentRow()?.itemName).toBe('A');
  });

  it('submits one request with the changed lines only', () => {
    http.bulkAdjustStock.mockReturnValue(of(undefined));
    const store = create();
    store.setSelectedLocationIds(['b']);
    store.setReason(AdjustmentReason.Miscount);
    const row = store.rowsInScope()[0];
    store.setCount(row, 7);
    store.submit();

    expect(http.bulkAdjustStock).toHaveBeenCalledExactlyOnceWith('c-1', {
      reason: AdjustmentReason.Miscount,
      lines: [{ itemId: 'i-A', locationId: 'b', actualQuantity: 7, expectedQuantity: row.quantity }]
    });
    expect(store.completed()).toBe(true);
  });

  it('keeps the values and exposes the error when saving fails', () => {
    http.bulkAdjustStock.mockReturnValue(throwError(() => ({ status: 409, title: 'Conflict' })));
    const store = create();
    store.setSelectedLocationIds(['b']);
    store.setCount(store.rowsInScope()[0], 7);
    store.submit();

    expect(store.completed()).toBe(false);
    expect(store.stockCountProblemDetail()).toEqual({ status: 409, title: 'Conflict' });
    expect(store.changes().length).toBe(1);
    expect(store.stockCountLoading()).toBe(false);
  });

  it('reloads rows after a concurrency conflict and reconciles the counts', () => {
    const conflict = {
      status: 409,
      title: 'Conflict',
      error: { code: ErrorCodes.stock.bulkConcurrencyConflict }
    };
    http.bulkAdjustStock.mockReturnValue(throwError(() => conflict));
    const store = create();
    store.setSelectedLocationIds(['a', 'b']);
    const [y, z, a, b] = store.rowsInScope();
    store.setCount(y, 9);
    store.setCount(z, 1);
    store.setCount(a, 1);
    store.setCount(b, 1);

    // Y changed server-side, Z now equals the count, B vanished
    http.getClassLocationStock.mockClear();
    http.getClassLocationStock.mockReturnValue(
      of({
        items: [row('Y', 'a', 6, 'Acat'), row('Z', 'a', 1, 'Zcat'), row('A', 'b', 3)],
        hasExpiredItems: false
      })
    );
    store.submit();

    expect(http.getClassLocationStock).toHaveBeenCalledTimes(1);
    expect(store.rows().find((r) => r.itemName === 'Y')?.quantity).toBe(6);
    expect(store.counts()).toEqual({ 'i-Y:a': 9, 'i-A:b': 1 });
    expect(store.stockCountProblemDetail()).toEqual(conflict);
    expect(store.completed()).toBe(false);
    expect(store.stockCountLoading()).toBe(false);
    expect(store.currentIndex()).toBe(0);
  });
});
