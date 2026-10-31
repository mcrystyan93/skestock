import { TestBed } from '@angular/core/testing';
import { type CongregationMutationDto, type CreateCongregationRequest } from '@ske/models';
import { of, throwError } from 'rxjs';
import { CongregationDetailState, NEW_CONGREGATION_ROUTE_ID } from './congregation-detail.store';
import { CongregationsHttp } from './congregations.http';

describe('CongregationDetailState', () => {
  const request: CreateCongregationRequest = {
    name: 'Central',
    email: 'central@example.com',
    canReceiveStudents: false,
    isSmallCongregation: false,
  };
  const saved: CongregationMutationDto = { id: 'congregation-1', ...request };

  let http: {
    getById: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    http = { getById: vi.fn(), create: vi.fn(), update: vi.fn() };
    TestBed.configureTestingModule({
      providers: [CongregationDetailState, { provide: CongregationsHttp, useValue: http }],
    });
  });

  it('starts from the prefill without calling the API for a new congregation', () => {
    const store = TestBed.inject(CongregationDetailState);

    store.loadCongregation({ id: NEW_CONGREGATION_ROUTE_ID, prefill: { name: 'Prefilled' } });

    expect(http.getById).not.toHaveBeenCalled();
    expect(store.congregation().name).toBe('Prefilled');
  });

  it('creates when the congregation has no id', () => {
    http.create.mockReturnValue(of(saved));
    const store = TestBed.inject(CongregationDetailState);

    store.saveCongregation(request);

    expect(http.create).toHaveBeenCalledWith(request);
    expect(http.update).not.toHaveBeenCalled();
    expect(store.congregation().id).toBe(saved.id);
  });

  it('updates the loaded congregation when it already has an id', () => {
    http.getById.mockReturnValue(of({ ...saved, createdDate: '', lastModifiedDate: '' }));
    http.update.mockReturnValue(of({ ...saved, name: 'Renamed' }));
    const store = TestBed.inject(CongregationDetailState);
    store.loadCongregation({ id: saved.id });

    store.saveCongregation({ ...request, name: 'Renamed' });

    expect(http.update).toHaveBeenCalledWith(saved.id, { ...request, name: 'Renamed' });
    expect(http.create).not.toHaveBeenCalled();
    expect(store.congregation().name).toBe('Renamed');
  });

  it('stops loading and keeps the congregation unchanged when saving fails', () => {
    http.create.mockReturnValue(throwError(() => ({ status: 400 })));
    const store = TestBed.inject(CongregationDetailState);

    store.saveCongregation(request);

    expect(store.congregationLoading()).toBe(false);
    expect(store.congregation().id).toBeUndefined();
  });
});
