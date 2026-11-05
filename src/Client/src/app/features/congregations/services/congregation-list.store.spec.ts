import { TestBed } from '@angular/core/testing';
import { Dispatcher } from '@ngrx/signals/events';
import { of, Subject, throwError } from 'rxjs';
import { type CongregationDto } from '@ske/models';
import { CongregationsHttp } from '@ske/shared/congregations';
import { realtimeEvents } from '@ske/signalr';
import { CongregationListState } from './congregation-list.store';

describe('CongregationListState', () => {
  const congregation: CongregationDto = {
    id: 'congregation-1',
    name: 'Central',
    email: 'central@example.com',
    canReceiveStudents: true,
    isSmallCongregation: false,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };
  const page = { data: [congregation], hasNextPage: false, nextCursor: null, sort: [] };
  const emptyPage = { data: [], hasNextPage: false, nextCursor: null, sort: [] };

  let http: { getAll: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };

  function createLoadedStore() {
    const store = TestBed.inject(CongregationListState);
    store.load({ searchTerm: null, filters: [], sort: [], cursor: null, pageSize: 10 });
    return store;
  }

  beforeEach(() => {
    http = { getAll: vi.fn().mockReturnValue(of(page)), delete: vi.fn() };
    TestBed.configureTestingModule({
      providers: [CongregationListState, { provide: CongregationsHttp, useValue: http }]
    });
  });

  it('loads the first page when the store is created', () => {
    const store = TestBed.inject(CongregationListState);

    expect(http.getAll).toHaveBeenCalledTimes(1);
    expect(store.congregations()).toEqual([congregation]);
  });

  it('deletes through the API and reloads the list', () => {
    const store = createLoadedStore();
    http.delete.mockReturnValue(of(undefined));
    http.getAll.mockReturnValue(of(emptyPage));

    store.deleteCongregation(congregation);

    expect(http.delete).toHaveBeenCalledWith(congregation.id);
    expect(store.congregations()).toEqual([]);
    expect(store.deletingCongregationId()).toBeNull();
  });

  it('tracks the congregation being deleted until the request completes', () => {
    const store = createLoadedStore();
    const pending = new Subject<void>();
    http.delete.mockReturnValue(pending);

    store.deleteCongregation(congregation);
    expect(store.deletingCongregationId()).toBe(congregation.id);

    pending.next();
    pending.complete();
    expect(store.deletingCongregationId()).toBeNull();
  });

  it('keeps the list and exposes the error when the delete fails', () => {
    const store = createLoadedStore();
    http.delete.mockReturnValue(throwError(() => ({ status: 404 })));
    http.getAll.mockClear();

    store.deleteCongregation(congregation);

    expect(store.congregations()).toEqual([congregation]);
    expect(store.deletingCongregationId()).toBeNull();
    expect(http.getAll).not.toHaveBeenCalled();
  });

  it('reloads the list when another user changes a congregation', () => {
    createLoadedStore();
    http.getAll.mockClear();

    TestBed.inject(Dispatcher).dispatch(realtimeEvents.congregationDeleted({ congregationId: 'other' }));

    expect(http.getAll).toHaveBeenCalledTimes(1);
  });
});
