import { TestBed } from '@angular/core/testing';
import { type LocationDto } from '@ske/models';
import { of, throwError } from 'rxjs';
import { LocationDropdownStore } from './location-dropdown.store';
import { LocationsHttp } from './locations.http';

describe('LocationDropdownStore', () => {
  const location: LocationDto = {
    id: 'location-1',
    name: 'Depozit',
    type: 'warehouse',
    isDefault: false,
    parentLocationId: null,
    parentLocationName: null,
    createdByName: null,
    lastModifiedByName: null,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };

  let http: {
    getAll: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    getDefault: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    http = {
      getAll: vi.fn(),
      getById: vi.fn(),
      getDefault: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        LocationDropdownStore,
        { provide: LocationsHttp, useValue: http }
      ]
    });
  });

  it('hydrates an Id-only value into the selected location', () => {
    http.getById.mockReturnValue(of(location));

    const store = TestBed.inject(LocationDropdownStore);
    store.resolveSelectedLocation({ id: location.id });

    expect(http.getById).toHaveBeenCalledWith(location.id);
    expect(store.selectedLocation()).toEqual(location);
    expect(store.selectedLocationLoading()).toBe(false);
    expect(store.selectedLocationUnavailable()).toBe(false);
  });

  it('exposes an unavailable state when hydration fails', () => {
    http.getById.mockReturnValue(throwError(() => ({ status: 404 })));

    const store = TestBed.inject(LocationDropdownStore);
    store.resolveSelectedLocation({ id: location.id });

    expect(store.selectedLocation()).toBeNull();
    expect(store.selectedLocationLoading()).toBe(false);
    expect(store.selectedLocationUnavailable()).toBe(true);
  });
});
