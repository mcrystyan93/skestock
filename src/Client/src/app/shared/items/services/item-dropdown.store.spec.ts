import { TestBed } from '@angular/core/testing';
import { type ItemDto } from '@ske/models';
import { of, throwError } from 'rxjs';
import { ItemDropdownStore } from './item-dropdown.store';
import { ItemsHttp } from './items.http';

describe('ItemDropdownStore', () => {
  const item: ItemDto = {
    id: 'item-1',
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
    createdByName: null,
    lastModifiedByName: null,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };

  let http: {
    getAll: ReturnType<typeof vi.fn>;
    getByIdCached: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    http = {
      getAll: vi.fn(),
      getByIdCached: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        ItemDropdownStore,
        { provide: ItemsHttp, useValue: http }
      ]
    });
  });

  it('hydrates an Id-only value into the selected item', () => {
    http.getByIdCached.mockReturnValue(of(item));

    const store = TestBed.inject(ItemDropdownStore);
    store.resolveSelectedItem({ id: item.id });

    expect(http.getByIdCached).toHaveBeenCalledWith(item.id);
    expect(store.selectedItem()).toEqual(item);
    expect(store.selectedItemLoading()).toBe(false);
    expect(store.selectedItemUnavailable()).toBe(false);
  });

  it('keeps the resolved selected item separate from the collection', () => {
    http.getByIdCached.mockReturnValue(of(item));
    http.getAll.mockReturnValue(of({
      data: [],
      hasNextPage: false,
      nextCursor: null,
      sort: []
    }));

    const store = TestBed.inject(ItemDropdownStore);
    store.resolveSelectedItem({ id: item.id });
    store.load({
      searchTerm: null,
      filters: [],
      sort: [],
      cursor: null,
      pageSize: 10
    });

    expect(store.items()).toEqual([]);
    expect(store.selectedItem()).toEqual(item);
  });

  it('exposes an unavailable state when hydration fails', () => {
    http.getByIdCached.mockReturnValue(throwError(() => ({ status: 404 })));

    const store = TestBed.inject(ItemDropdownStore);
    store.resolveSelectedItem({ id: item.id });

    expect(store.selectedItem()).toBeNull();
    expect(store.selectedItemLoading()).toBe(false);
    expect(store.selectedItemUnavailable()).toBe(true);
  });
});
