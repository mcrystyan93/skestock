import { TestBed } from '@angular/core/testing';
import { type CategoryDto } from '@ske/models';
import { of, throwError } from 'rxjs';
import { CategoryDropdownStore } from './category-dropdown.store';
import { CategoriesHttp } from './categories.http';

describe('CategoryDropdownStore', () => {
  const category: CategoryDto = {
    id: 'category-1',
    name: 'Papetărie',
    itemCount: 2,
    createdByName: null,
    lastModifiedByName: null,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z',
    icon: null,
  };

  let http: {
    getAll: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    http = {
      getAll: vi.fn(),
      getById: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [CategoryDropdownStore, { provide: CategoriesHttp, useValue: http }],
    });
  });

  it('hydrates an Id-only value into the selected category', () => {
    http.getById.mockReturnValue(of(category));

    const store = TestBed.inject(CategoryDropdownStore);
    store.resolveSelectedCategory({ id: category.id });

    expect(http.getById).toHaveBeenCalledWith(category.id);
    expect(store.selectedCategory()).toEqual(category);
    expect(store.selectedCategoryLoading()).toBe(false);
    expect(store.selectedCategoryUnavailable()).toBe(false);
  });

  it('keeps the resolved selected category separate from the collection', () => {
    http.getById.mockReturnValue(of(category));
    http.getAll.mockReturnValue(
      of({
        data: [],
        hasNextPage: false,
        nextCursor: null,
        sort: [],
      }),
    );

    const store = TestBed.inject(CategoryDropdownStore);
    store.resolveSelectedCategory({ id: category.id });
    store.load({
      searchTerm: null,
      filters: [],
      sort: [],
      cursor: null,
      pageSize: 10,
    });

    expect(store.categories()).toEqual([]);
    expect(store.selectedCategory()).toEqual(category);
  });

  it('exposes an unavailable state when hydration fails', () => {
    http.getById.mockReturnValue(throwError(() => ({ status: 404 })));

    const store = TestBed.inject(CategoryDropdownStore);
    store.resolveSelectedCategory({ id: category.id });

    expect(store.selectedCategory()).toBeNull();
    expect(store.selectedCategoryLoading()).toBe(false);
    expect(store.selectedCategoryUnavailable()).toBe(true);
  });
});
