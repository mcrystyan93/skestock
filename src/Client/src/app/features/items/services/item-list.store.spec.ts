import { TestBed } from '@angular/core/testing';
import { provideDispatcher } from '@ngrx/signals/events';
import { type ItemDto } from '@ske/models';
import { ItemsHttp } from '@ske/shared/items';
import { of, Subject, throwError } from 'rxjs';
import { ItemListState } from './item-list.store';

describe('ItemListState', () => {
  let http: {
    getAll: ReturnType<typeof vi.fn>;
    edit: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    http = { getAll: vi.fn(), edit: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideDispatcher(),
        ItemListState,
        { provide: ItemsHttp, useValue: http },
      ],
    });
  });

  it('saves the picked image with the current row fields and replaces only that row', () => {
    const item = createItem('item-1');
    const other = createItem('item-2');
    http.getAll.mockReturnValue(of({ data: [item, other], hasNextPage: false, nextCursor: null, sort: [] }));
    http.edit.mockReturnValue(of({ ...item, imageFileId: 'file-1' }));
    const store = TestBed.inject(ItemListState);

    store.load(store.filter());
    store.setItemImage({ item, fileId: 'file-1' });

    expect(http.edit).toHaveBeenCalledWith('item-1', {
      sku: 'CR-1',
      name: 'Creion',
      description: null,
      unit: 'bucată',
      minThreshold: 1,
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
    http.getAll.mockReturnValue(of({ data: [first, second], hasNextPage: false, nextCursor: null, sort: [] }));
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
    http.getAll.mockReturnValue(of({ data: [item], hasNextPage: false, nextCursor: null, sort: [] }));
    http.edit.mockReturnValue(throwError(() => ({ status: 500 })));
    const store = TestBed.inject(ItemListState);

    store.load(store.filter());
    store.setItemImage({ item, fileId: 'file-1' });

    expect(store.items().map((i) => i.imageFileId)).toEqual([null]);
    expect(store.itemsProblemDetail()).toEqual(expect.objectContaining({ status: 500 }));
  });
});

function createItem(id: string): ItemDto {
  return {
    id,
    name: 'Creion',
    sku: 'CR-1',
    description: null,
    unit: 'bucată',
    minThreshold: 1,
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
