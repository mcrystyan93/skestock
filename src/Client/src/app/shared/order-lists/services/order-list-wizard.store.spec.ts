import { TestBed } from '@angular/core/testing';
import { OrderCatalogItemDto, SupplyListDto } from '@ske/models';
import { SupplyListsHttp } from '@ske/shared/supply-lists';
import { of, throwError } from 'rxjs';
import { OrderListWizardStore } from './order-list-wizard.store';
import { OrderListsHttp } from './order-lists.http';
import { supplyListsToLines } from './order-list-wizard.utils';

const catalogItem = (itemId: string, lastQuantity: number | null = null): OrderCatalogItemDto => ({
  itemId,
  name: `Item ${itemId}`,
  sku: `SKU-${itemId}`,
  imageFileId: `img-${itemId}`,
  unit: 'buc',
  lastQuantity,
  averageIntervalDays: null,
  isFrequent: false,
  locations: []
});

const supplyList: SupplyListDto = {
  id: 'list-1',
  name: 'Lista lunară',
  note: 'Notă',
  frequency: 'Monthly',
  isActive: true,
  lines: [{ id: 'l1', itemId: 'i1', itemImageFileId: 'img-i1', itemName: 'Creioane', itemSku: null, quantity: 12, unit: 'set', notes: 'colorate' }],
  createdDate: '',
  lastModifiedDate: ''
};

const secondSupplyList: SupplyListDto = {
  ...supplyList,
  id: 'list-2',
  name: 'Lista anuală',
  note: null,
  lines: [
    { id: 'm1', itemId: 'i1', itemImageFileId: 'img-i1', itemName: 'Creioane', itemSku: null, quantity: 3, unit: 'set', notes: 'ascuțite' },
    { id: 'm2', itemId: 'i2', itemImageFileId: null, itemName: 'Riglă', itemSku: null, quantity: 4, unit: 'buc', notes: null }
  ]
};

describe('OrderListWizardStore', () => {
  let orderListsHttp: { getCatalog: ReturnType<typeof vi.fn> };
  let supplyListsHttp: {
    getAll: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    getAllActive: () => unknown;
  };

  beforeEach(() => {
    orderListsHttp = { getCatalog: vi.fn() };
    supplyListsHttp = {
      getAll: vi.fn(),
      getById: vi.fn(),
      getAllActive: () => SupplyListsHttp.prototype.getAllActive.call(supplyListsHttp)
    };

    TestBed.configureTestingModule({
      providers: [
        OrderListWizardStore,
        { provide: OrderListsHttp, useValue: orderListsHttp },
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

  it('loads the catalog once when leaving the source step and starts with nothing selected', () => {
    orderListsHttp.getCatalog.mockReturnValue(of([catalogItem('1'), catalogItem('2')]));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('new');
    store.advance('class-1');

    expect(store.step()).toBe('selection');
    expect(orderListsHttp.getCatalog).toHaveBeenCalledTimes(1);
    expect(orderListsHttp.getCatalog).toHaveBeenCalledWith('class-1');
    expect(store.catalog().map((item) => item.itemId)).toEqual(['1', '2']);
    expect(store.selectedItemIds()).toEqual([]);
  });

  it('builds prefill lines with the last quantity, or 1 without history, only for the selected items', () => {
    orderListsHttp.getCatalog.mockReturnValue(of([catalogItem('1', 5), catalogItem('2', 3.4), catalogItem('3')]));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('new');
    store.advance('class-1');
    store.setSelectedItemIds(['2', '3']);

    expect(store.buildPrefill().lines).toEqual([
      { id: null, itemId: '2', itemImageFileId: 'img-2', productName: '(SKU-2) Item 2', quantity: 3, unit: 'buc', notes: '' },
      { id: null, itemId: '3', itemImageFileId: 'img-3', productName: '(SKU-3) Item 3', quantity: 1, unit: 'buc', notes: '' }
    ]);
  });

  it('does not load the catalog for the supply list source', () => {
    supplyListsHttp.getAll.mockReturnValue(of({ data: [], hasNextPage: false, sort: [] }));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('supplyList');
    store.advance('class-1');

    expect(orderListsHttp.getCatalog).not.toHaveBeenCalled();
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
    store.advance('class-1');
    expect(store.step()).toBe('products');
    store.advance('class-1');
    expect(store.step()).toBe('details');
    expect(store.buildPrefill().note).toBe('Notă');
    store.back();
    store.back();

    store.selectSupplyLists(['list-1', 'list-2']);
    expect(store.distinctSelectedItemCount()).toBe(2);
    store.advance('class-1');
    store.advance('class-1');

    expect(store.buildPrefill()).toEqual({
      name: 'Lista lunară + Lista anuală',
      note: '',
      lines: [
        { id: null, itemId: 'i1', itemImageFileId: 'img-i1', productName: 'Creioane', quantity: 15, unit: 'set', notes: 'colorate · ascuțite' },
        { id: null, itemId: 'i2', itemImageFileId: null, productName: 'Riglă', quantity: 4, unit: 'buc', notes: '' }
      ]
    });

    store.back();
    store.selectSupplyLists([]);
    expect(store.canGoNext()).toBe(false);
  });

  it('drops unticked products from the prefill, resets them when the lists change and needs one product', () => {
    supplyListsHttp.getAll.mockReturnValue(of({ data: [{ id: 'list-1' }, { id: 'list-2' }], hasNextPage: false, sort: [] }));
    supplyListsHttp.getById.mockImplementation((id: string) => of(id === 'list-1' ? supplyList : secondSupplyList));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('supplyList');
    store.advance('class-1');
    store.selectSupplyLists(['list-1', 'list-2']);
    store.advance('class-1');
    expect(store.step()).toBe('products');
    expect(store.selectedProductIds()).toEqual(['i1', 'i2']);

    store.setSelectedProductIds(['i2']);
    expect(store.buildPrefill().lines?.map((line) => line.itemId)).toEqual(['i2']);

    store.setSelectedProductIds([]);
    expect(store.canGoNext()).toBe(false);

    store.setSelectedProductIds(['i2']);
    store.advance('class-1');
    store.back();
    store.back();
    expect(store.step()).toBe('selection');
    store.selectSupplyLists(['list-1']);
    expect(store.selectedProductIds()).toEqual(['i1']);
  });

  it('keeps the typed name when only the ticked products change', () => {
    supplyListsHttp.getAll.mockReturnValue(of({ data: [{ id: 'list-1' }, { id: 'list-2' }], hasNextPage: false, sort: [] }));
    supplyListsHttp.getById.mockImplementation((id: string) => of(id === 'list-1' ? supplyList : secondSupplyList));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('supplyList');
    store.advance('class-1');
    store.selectSupplyLists(['list-1', 'list-2']);
    store.advance('class-1');
    store.advance('class-1');
    store.setDetails({ name: 'Numele meu', note: '' });
    store.back();
    store.setSelectedProductIds(['i2']);
    store.back();
    store.advance('class-1');

    expect(store.name()).toBe('Numele meu');
  });

  it('skips the products step for the catalog source', () => {
    orderListsHttp.getCatalog.mockReturnValue(of([catalogItem('1')]));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('new');
    store.advance('class-1');
    store.advance('class-1');

    expect(store.step()).toBe('details');
    expect(store.stepIndex()).toBe(2);
  });

  it('needs a name on the details step and keeps what was typed when the selection is unchanged', () => {
    orderListsHttp.getCatalog.mockReturnValue(of([]));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('new');
    store.advance('class-1');
    store.advance('class-1');
    expect(store.step()).toBe('details');
    expect(store.canGoNext()).toBe(false);

    store.setDetails({ name: ' Comandă ', note: 'urgent' });
    expect(store.canGoNext()).toBe(true);
    expect(store.buildPrefill()).toEqual({ name: 'Comandă', note: 'urgent', lines: [] });

    store.back();
    store.advance('class-1');
    expect(store.name()).toBe(' Comandă ');
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
    orderListsHttp.getCatalog.mockReturnValue(of([]));
    store.advance('class-1');
    store.advance('class-1');
    store.setDetails({ name: 'Comandă', note: '' });
    store.advance('class-1');
    expect(store.step()).toBe('edit');

    store.back();
    store.back();
    store.back();
    store.back();
    expect(store.step()).toBe('source');
  });

  it('keeps the prefill key stable until the selection changes', () => {
    orderListsHttp.getCatalog.mockReturnValue(of([catalogItem('1'), catalogItem('2')]));
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
    orderListsHttp.getCatalog.mockReturnValueOnce(throwError(() => new Error('down')));
    const store = TestBed.inject(OrderListWizardStore);

    store.setSource('new');
    store.advance('class-1');
    expect(store.loadFailed()).toBe(true);
    expect(store.canGoNext()).toBe(false);

    orderListsHttp.getCatalog.mockReturnValue(of([catalogItem('1')]));
    store.retry('class-1');

    expect(store.loadFailed()).toBe(false);
    expect(store.canGoNext()).toBe(true);
  });
});
