import { TestBed } from '@angular/core/testing';
import { LowStockItemDto, SupplyListDto } from '@ske/models';
import { StockHttp } from '@ske/shared/stock';
import { SupplyListsHttp } from '@ske/shared/supply-lists';
import { of, throwError } from 'rxjs';
import { OrderListWizardStore } from './order-list-wizard.store';
import { supplyListsToLines } from './order-list-wizard.utils';

const lowStock = (itemId: string, locationName: string): LowStockItemDto => ({
  itemId,
  itemName: `Item ${itemId}`,
  sku: `SKU-${itemId}`,
  unit: 'buc',
  locationName
});

const supplyList: SupplyListDto = {
  id: 'list-1',
  name: 'Lista lunară',
  note: 'Notă',
  frequency: 'Monthly',
  isActive: true,
  lines: [{ id: 'l1', itemId: 'i1', itemName: 'Creioane', itemSku: null, quantity: 12, unit: 'set', notes: 'colorate' }],
  createdDate: '',
  lastModifiedDate: ''
};

const secondSupplyList: SupplyListDto = {
  ...supplyList,
  id: 'list-2',
  name: 'Lista anuală',
  note: null,
  lines: [
    { id: 'm1', itemId: 'i1', itemName: 'Creioane', itemSku: null, quantity: 3, unit: 'set', notes: 'ascuțite' },
    { id: 'm2', itemId: 'i2', itemName: 'Riglă', itemSku: null, quantity: 4, unit: 'buc', notes: null }
  ]
};

describe('OrderListWizardStore', () => {
  let stockHttp: { getLowStockItems: ReturnType<typeof vi.fn> };
  let supplyListsHttp: { getAll: ReturnType<typeof vi.fn>; getById: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    stockHttp = { getLowStockItems: vi.fn() };
    supplyListsHttp = { getAll: vi.fn(), getById: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        OrderListWizardStore,
        { provide: StockHttp, useValue: stockHttp },
        { provide: SupplyListsHttp, useValue: supplyListsHttp }
      ]
    });
  });

  it('cannot leave the first step until a source is chosen', () => {
    const store = TestBed.inject(OrderListWizardStore);

    expect(store.advance('class-1')).toBe(false);
    expect(store.step()).toBe('source');
    expect(store.canGoNext()).toBe(false);

    store.setSource('new');
    expect(store.canGoNext()).toBe(true);
  });

  it('suggests an item low in several locations once and selects everything by default', () => {
    stockHttp.getLowStockItems.mockReturnValue(of([
      lowStock('1', 'Depozit'),
      lowStock('1', 'Birou'),
      lowStock('2', 'Depozit')
    ]));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('new');
    store.advance('class-1');

    expect(store.step()).toBe('selection');
    expect(stockHttp.getLowStockItems).toHaveBeenCalledWith('class-1');
    expect(store.suggestions().map(item => item.itemId)).toEqual(['1', '2']);
    expect(store.suggestions()[0].locations).toEqual(['Birou', 'Depozit']);
    expect(store.selectedItemIds()).toEqual(['1', '2']);
  });

  it('builds prefill lines with quantity 1 only for the selected suggestions', () => {
    stockHttp.getLowStockItems.mockReturnValue(of([lowStock('1', 'A'), lowStock('2', 'A')]));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('new');
    store.advance('class-1');
    store.setSelectedItemIds(['2']);

    const { lines } = store.buildPrefill();
    expect(lines).toEqual([
      { id: null, itemId: '2', productName: '(SKU-2) Item 2', quantity: 1, unit: 'buc', notes: '' }
    ]);
  });

  it('requires at least one supply list and merges the selected ones by item', () => {
    supplyListsHttp.getAll.mockReturnValue(of({ data: [{ id: 'list-1' }, { id: 'list-2' }], hasNextPage: false, sort: [] }));
    supplyListsHttp.getById.mockImplementation((id: string) => of(id === 'list-1' ? supplyList : secondSupplyList));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('supplyList');
    store.advance('class-1');
    expect(store.step()).toBe('selection');
    expect(store.supplyLists().length).toBe(2);
    expect(store.canGoNext()).toBe(false);

    store.selectSupplyLists(['list-1']);
    expect(store.canGoNext()).toBe(true);
    expect(store.buildPrefill().note).toBe('Notă');

    store.selectSupplyLists(['list-1', 'list-2']);
    expect(store.distinctSelectedItemCount()).toBe(2);

    expect(store.buildPrefill()).toEqual({
      name: 'Lista lunară + Lista anuală',
      note: null,
      lines: [
        { id: null, itemId: 'i1', productName: 'Creioane', quantity: 15, unit: 'set', notes: 'colorate · ascuțite' },
        { id: null, itemId: 'i2', productName: 'Riglă', quantity: 4, unit: 'buc', notes: '' }
      ]
    });

    store.selectSupplyLists([]);
    expect(store.canGoNext()).toBe(false);
  });

  it('reads every page of active supply lists using the API page size', () => {
    supplyListsHttp.getAll
      .mockReturnValueOnce(of({ data: [{ id: 'a' }], hasNextPage: true, nextCursor: 'c2', sort: [] }))
      .mockReturnValueOnce(of({ data: [{ id: 'b' }], hasNextPage: false, sort: [] }));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('supplyList');
    store.advance('class-1');

    expect(supplyListsHttp.getAll).toHaveBeenCalledTimes(2);
    expect(supplyListsHttp.getAll.mock.calls[0][0].pageSize).toBe(50);
    expect(supplyListsHttp.getAll.mock.calls[1][0].cursor).toBe('c2');
    expect(store.supplyLists().map(list => list.id)).toEqual(['a', 'b']);
  });

  it('goes back one step at a time without going below zero', () => {
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('new');
    stockHttp.getLowStockItems.mockReturnValue(of([]));
    store.advance('class-1');
    store.advance('class-1');
    expect(store.step()).toBe('edit');

    store.back();
    store.back();
    store.back();
    expect(store.step()).toBe('source');
  });

  it('keeps the prefill key stable until the selection changes', () => {
    stockHttp.getLowStockItems.mockReturnValue(of([lowStock('1', 'A'), lowStock('2', 'A')]));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('new');
    store.advance('class-1');
    store.markPrefillLoaded();
    expect(store.prefillKey()).toBe(store.loadedPrefillKey());

    store.setSelectedItemIds(['1']);
    expect(store.prefillKey()).not.toBe(store.loadedPrefillKey());
  });

  it('retries the details of the chosen supply lists when only they failed', () => {
    supplyListsHttp.getAll.mockReturnValue(of({ data: [{ id: 'list-1' }], hasNextPage: false, sort: [] }));
    supplyListsHttp.getById.mockReturnValue(of(supplyList));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('supplyList');
    store.advance('class-1');
    store.selectSupplyLists(['list-1']);
    supplyListsHttp.getById.mockClear();

    store.retry('class-1');

    expect(supplyListsHttp.getAll).toHaveBeenCalledTimes(1);
    expect(supplyListsHttp.getById).toHaveBeenCalledWith('list-1');
  });

  it('joins merged notes without cutting a note in the middle', () => {
    const long = 'x'.repeat(600);
    const lists: SupplyListDto[] = [
      { ...supplyList, lines: [{ ...supplyList.lines[0], notes: long }] },
      { ...supplyList, id: 'l2', lines: [{ ...supplyList.lines[0], notes: 'y'.repeat(600) }] }
    ];

    const [line] = supplyListsToLines(lists);

    expect(line.notes).toBe(long);
  });

  it('blocks Continue while the suggestions failed to load and allows it after a retry', () => {
    stockHttp.getLowStockItems.mockReturnValueOnce(throwError(() => new Error('down')));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('new');
    store.advance('class-1');
    expect(store.loadFailed()).toBe(true);
    expect(store.canGoNext()).toBe(false);

    stockHttp.getLowStockItems.mockReturnValue(of([lowStock('1', 'A')]));
    store.retry('class-1');

    expect(store.loadFailed()).toBe(false);
    expect(store.canGoNext()).toBe(true);
  });
});
