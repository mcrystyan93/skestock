import { TestBed } from '@angular/core/testing';
import { type CongregationDto } from '@ske/models';
import { of, throwError } from 'rxjs';
import { CongregationDropdownStore } from './congregation-dropdown.store';
import { CongregationsHttp } from './congregations.http';

describe('CongregationDropdownStore', () => {
  const congregation: CongregationDto = {
    id: 'congregation-1',
    name: 'Central',
    email: 'central@example.com',
    canReceiveStudents: true,
    isSmallCongregation: false,
    createdByName: null,
    lastModifiedByName: null,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z',
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
      providers: [CongregationDropdownStore, { provide: CongregationsHttp, useValue: http }],
    });
  });

  it('hydrates an Id-only value into the selected congregation', () => {
    http.getById.mockReturnValue(of(congregation));

    const store = TestBed.inject(CongregationDropdownStore);
    store.resolveSelectedCongregation({ id: congregation.id });

    expect(http.getById).toHaveBeenCalledWith(congregation.id);
    expect(store.selectedCongregation()).toEqual(congregation);
    expect(store.selectedCongregationLoading()).toBe(false);
    expect(store.selectedCongregationUnavailable()).toBe(false);
  });

  it('keeps the resolved selected congregation separate from the collection', () => {
    http.getById.mockReturnValue(of(congregation));
    http.getAll.mockReturnValue(
      of({
        data: [],
        hasNextPage: false,
        nextCursor: null,
        sort: [],
      }),
    );

    const store = TestBed.inject(CongregationDropdownStore);
    store.resolveSelectedCongregation({ id: congregation.id });
    store.load({
      searchTerm: null,
      filters: [],
      sort: [],
      cursor: null,
      pageSize: 10,
    });

    expect(store.congregations()).toEqual([]);
    expect(store.selectedCongregation()).toEqual(congregation);
  });

  it('exposes an unavailable state when hydration fails', () => {
    http.getById.mockReturnValue(throwError(() => ({ status: 404 })));

    const store = TestBed.inject(CongregationDropdownStore);
    store.resolveSelectedCongregation({ id: congregation.id });

    expect(store.selectedCongregation()).toBeNull();
    expect(store.selectedCongregationLoading()).toBe(false);
    expect(store.selectedCongregationUnavailable()).toBe(true);
  });
});
