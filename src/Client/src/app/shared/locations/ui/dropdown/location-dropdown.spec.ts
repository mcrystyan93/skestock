import { TestBed } from '@angular/core/testing';
import { type LocationDto } from '@ske/models';
import { of, Subject, throwError } from 'rxjs';
import { LocationDropdown } from './location-dropdown';
import { LocationsHttp } from '../../services/locations.http';

describe('LocationDropdown', () => {
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
      imports: [LocationDropdown],
      providers: [{ provide: LocationsHttp, useValue: http }]
    });
  });

  it('shows a loading label before resolving an Id-only value', () => {
    const pendingLocation = new Subject<LocationDto>();
    http.getById.mockReturnValue(pendingLocation);

    const fixture = TestBed.createComponent(LocationDropdown);
    const dropdown = fixture.componentInstance;
    dropdown.value.set({ id: location.id });
    fixture.detectChanges();

    expect(dropdown.selectedLocationLabel()).toBe('Se încarcă locația…');

    pendingLocation.next(location);
    pendingLocation.complete();
    fixture.detectChanges();

    expect(dropdown.selectedLocationLabel()).toBe(location.name);
    fixture.destroy();
  });

  it('shows an explicit fallback when the Id cannot be resolved', () => {
    http.getById.mockReturnValue(throwError(() => ({ status: 404 })));

    const fixture = TestBed.createComponent(LocationDropdown);
    const dropdown = fixture.componentInstance;
    dropdown.value.set({ id: location.id });
    fixture.detectChanges();

    expect(dropdown.selectedLocationLabel()).toBe(`Locație indisponibilă`);
    fixture.destroy();
  });

  it('uses a supplied name without making a detail request', () => {
    http.getById.mockReturnValue(of(location));

    const fixture = TestBed.createComponent(LocationDropdown);
    const dropdown = fixture.componentInstance;
    dropdown.value.set(location);
    fixture.detectChanges();

    expect(dropdown.selectedLocationLabel()).toBe(location.name);
    expect(http.getById).not.toHaveBeenCalled();
    fixture.destroy();
  });
});
