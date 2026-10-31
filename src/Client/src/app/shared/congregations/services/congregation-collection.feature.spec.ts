import { TestBed } from '@angular/core/testing';
import { signalStore } from '@ngrx/signals';
import { Dispatcher } from '@ngrx/signals/events';
import { of } from 'rxjs';
import { type CongregationDto } from '@ske/models';
import { realtimeEvents } from '@ske/signalr';
import { withCongregationCollection } from './congregation-collection.feature';
import { CongregationsHttp } from './congregations.http';

const CollectionStore = signalStore(withCongregationCollection());

describe('withCongregationCollection', () => {
  const congregation: CongregationDto = {
    id: 'congregation-1',
    name: 'Central',
    email: 'central@example.com',
    canReceiveStudents: true,
    isSmallCongregation: false,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z',
  };
  const page = { data: [congregation], hasNextPage: false, nextCursor: null, sort: [] };

  let http: { getAll: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    http = { getAll: vi.fn().mockReturnValue(of(page)) };
    TestBed.configureTestingModule({
      providers: [CollectionStore, { provide: CongregationsHttp, useValue: http }],
    });
  });

  it('loads the first page into the collection', () => {
    const store = TestBed.inject(CollectionStore);

    store.load({ searchTerm: null, filters: [], sort: [], cursor: null, pageSize: 10 });

    expect(store.congregations()).toEqual([congregation]);
    expect(store.hasNextPage()).toBe(false);
  });

  it.each([
    ['created', () => realtimeEvents.congregationCreated({ congregationId: 'x' })],
    ['updated', () => realtimeEvents.congregationUpdated({ congregationId: 'x' })],
    ['deleted', () => realtimeEvents.congregationDeleted({ congregationId: 'x' })],
  ])('refetches the first page when a congregation is %s', (_name, event) => {
    const store = TestBed.inject(CollectionStore);
    store.load({ searchTerm: null, filters: [], sort: [], cursor: null, pageSize: 10 });
    http.getAll.mockClear();

    TestBed.inject(Dispatcher).dispatch(event());

    expect(http.getAll).toHaveBeenCalledTimes(1);
  });

  it('ignores realtime events while nothing has been loaded', () => {
    TestBed.inject(CollectionStore);

    TestBed.inject(Dispatcher).dispatch(realtimeEvents.congregationCreated({ congregationId: 'x' }));

    expect(http.getAll).not.toHaveBeenCalled();
  });
});
