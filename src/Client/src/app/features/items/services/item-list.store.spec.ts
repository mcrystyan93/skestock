import { TestBed } from '@angular/core/testing';
import { Dispatcher, provideDispatcher } from '@ngrx/signals/events';
import { type ItemDto } from '@ske/models';
import { ItemsHttp } from '@ske/shared/items';
import { realtimeEvents } from '@ske/signalr';
import { of, Subject, throwError } from 'rxjs';
import { ItemListState } from './item-list.store';

describe('ItemListState', () => {
  let http: {
    getAll: ReturnType<typeof vi.fn>;
    edit: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    http = { getAll: vi.fn(), edit: vi.fn(), getById: vi.fn() };

    TestBed.configureTestingModule({
      providers: [provideDispatcher(), ItemListState, { provide: ItemsHttp, useValue: http }],
    });
  });

  it('saves the picked image with the current row fields and replaces only that row', () => {
    const item = createItem('item-1');
    const other = createItem('item-2');
    http.getAll.mockReturnValue(
      of({ data: [item, other], hasNextPage: false, nextCursor: null, sort: [] }),
    );
    http.edit.mockReturnValue(of({ ...item, imageFileId: 'file-1' }));
    const store = TestBed.inject(ItemListState);

    store.load(store.filter());
    store.setItemImage({ item, fileId: 'file-1' });

    expect(http.edit).toHaveBeenCalledWith('item-1', {
      sku: 'CR-1',
      name: 'Creion',
      description: null,
      unit: 'bucată',
      locationThresholds: [],
      isPerishable: false,
      shelfLifeDays: null,
      categoryId: 'category-1',
      imageFileId: 'file-1',
    });
    expect(store.items().map((i) => [i.id, i.imageFileId])).toEqual([
      ['item-1', 'file-1'],
      ['item-2', null],
    ]);
  });

  it('keeps an earlier save in flight when a later save for another row starts', () => {
    const first = createItem('item-1');
    const second = createItem('item-2');
    const firstSave = new Subject<ItemDto>();
    const secondSave = new Subject<ItemDto>();
    http.getAll.mockReturnValue(
      of({ data: [first, second], hasNextPage: false, nextCursor: null, sort: [] }),
    );
    http.edit.mockReturnValueOnce(firstSave).mockReturnValueOnce(secondSave);
    const store = TestBed.inject(ItemListState);

    store.load(store.filter());
    store.setItemImage({ item: first, fileId: 'file-1' });
    store.setItemImage({ item: second, fileId: 'file-2' });
    secondSave.next({ ...second, imageFileId: 'file-2' });
    secondSave.complete();
    firstSave.next({ ...first, imageFileId: 'file-1' });
    firstSave.complete();

    expect(store.items().map((i) => [i.id, i.imageFileId])).toEqual([
      ['item-1', 'file-1'],
      ['item-2', 'file-2'],
    ]);
  });

  it('keeps the row unchanged and reports the problem when the save fails', () => {
    const item = createItem('item-1');
    http.getAll.mockReturnValue(
      of({ data: [item], hasNextPage: false, nextCursor: null, sort: [] }),
    );
    http.edit.mockReturnValue(throwError(() => ({ status: 500 })));
    const store = TestBed.inject(ItemListState);

    store.load(store.filter());
    store.setItemImage({ item, fileId: 'file-1' });

    expect(store.items().map((i) => i.imageFileId)).toEqual([null]);
    expect(store.itemsProblemDetail()).toEqual(expect.objectContaining({ status: 500 }));
  });

  describe('realtime events', () => {
    const page = (...data: ItemDto[]) => ({ data, hasNextPage: false, nextCursor: null, sort: [] });

    it('replaces only the changed row without reloading the list', () => {
      const item = createItem('item-1');
      const other = createItem('item-2');
      http.getAll.mockReturnValue(of(page(item, other)));
      http.getById.mockReturnValue(of({ ...item, name: 'Pix' }));
      const store = TestBed.inject(ItemListState);
      store.load(store.filter());

      TestBed.inject(Dispatcher).dispatch(realtimeEvents.itemUpdated({ itemId: 'item-1' }));

      expect(http.getById).toHaveBeenCalledWith('item-1');
      expect(http.getAll).toHaveBeenCalledTimes(1);
      expect(store.items().map((i) => [i.id, i.name])).toEqual([
        ['item-1', 'Pix'],
        ['item-2', 'Creion'],
      ]);
    });

    it.each([
      ['itemEnabled', realtimeEvents.itemEnabled],
      ['itemDisabled', realtimeEvents.itemDisabled],
    ])('patches the row on %s', (_name, event) => {
      const item = createItem('item-1');
      http.getAll.mockReturnValue(of(page(item)));
      http.getById.mockReturnValue(of({ ...item, isActive: false }));
      const store = TestBed.inject(ItemListState);
      store.load(store.filter());

      TestBed.inject(Dispatcher).dispatch(event({ itemId: 'item-1' }));

      expect(store.items().map((i) => i.isActive)).toEqual([false]);
      expect(http.getAll).toHaveBeenCalledTimes(1);
    });

    it('ignores an item that is not in the loaded list', () => {
      http.getAll.mockReturnValue(of(page(createItem('item-1'))));
      const store = TestBed.inject(ItemListState);
      store.load(store.filter());

      TestBed.inject(Dispatcher).dispatch(realtimeEvents.itemUpdated({ itemId: 'item-9' }));

      expect(http.getById).not.toHaveBeenCalled();
      expect(http.getAll).toHaveBeenCalledTimes(1);
    });

    it('applies events for different items that are in flight together', () => {
      const first = createItem('item-1');
      const second = createItem('item-2');
      const firstFetch = new Subject<ItemDto>();
      const secondFetch = new Subject<ItemDto>();
      http.getAll.mockReturnValue(of(page(first, second)));
      http.getById.mockReturnValueOnce(firstFetch).mockReturnValueOnce(secondFetch);
      const store = TestBed.inject(ItemListState);
      store.load(store.filter());
      const dispatcher = TestBed.inject(Dispatcher);

      dispatcher.dispatch(realtimeEvents.itemUpdated({ itemId: 'item-1' }));
      dispatcher.dispatch(realtimeEvents.itemUpdated({ itemId: 'item-2' }));
      secondFetch.next({ ...second, name: 'B' });
      firstFetch.next({ ...first, name: 'A' });

      expect(store.items().map((i) => i.name)).toEqual(['A', 'B']);
    });

    it('lets the newest event for a row win when an older fetch resolves late', () => {
      const item = createItem('item-1');
      const olderFetch = new Subject<ItemDto>();
      const newerFetch = new Subject<ItemDto>();
      http.getAll.mockReturnValue(of(page(item)));
      http.getById.mockReturnValueOnce(olderFetch).mockReturnValueOnce(newerFetch);
      const store = TestBed.inject(ItemListState);
      store.load(store.filter());
      const dispatcher = TestBed.inject(Dispatcher);

      dispatcher.dispatch(realtimeEvents.itemDisabled({ itemId: 'item-1' }));
      dispatcher.dispatch(realtimeEvents.itemEnabled({ itemId: 'item-1' }));
      newerFetch.next({ ...item, isActive: true, name: 'Nou' });
      olderFetch.next({ ...item, isActive: false, name: 'Vechi' });

      expect(store.items().map((i) => i.name)).toEqual(['Nou']);
    });

    it('falls back to reloading the list when the row fetch fails', () => {
      const item = createItem('item-1');
      http.getAll.mockReturnValue(of(page(item)));
      http.getById.mockReturnValue(throwError(() => ({ status: 500 })));
      const store = TestBed.inject(ItemListState);
      store.load(store.filter());

      TestBed.inject(Dispatcher).dispatch(realtimeEvents.itemUpdated({ itemId: 'item-1' }));

      expect(http.getAll).toHaveBeenCalledTimes(2);
    });

    it('still reloads the list when an item is created', () => {
      http.getAll.mockReturnValue(of(page(createItem('item-1'))));
      const store = TestBed.inject(ItemListState);
      store.load(store.filter());

      TestBed.inject(Dispatcher).dispatch(realtimeEvents.itemCreated({ itemId: 'item-2' }));

      expect(http.getAll).toHaveBeenCalledTimes(2);
      expect(http.getById).not.toHaveBeenCalled();
    });
  });
});

function createItem(id: string): ItemDto {
  return {
    id,
    name: 'Creion',
    sku: 'CR-1',
    description: null,
    unit: 'bucată',
    locationThresholds: [],
    isPerishable: false,
    shelfLifeDays: null,
    isActive: true,
    categoryId: 'category-1',
    categoryName: 'Papetărie',
    imageFileId: null,
    createdByName: null,
    lastModifiedByName: null,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z',
  };
}
