import { TestBed } from '@angular/core/testing';
import { Events, provideDispatcher } from '@ngrx/signals/events';
import { type SaveSupplyListRequest, type SupplyListDto } from '@ske/models';
import { of, throwError } from 'rxjs';
import { supplyListApiEvents, SupplyListDetailState } from './supply-list-detail.store';
import { SupplyListsHttp } from './supply-lists.http';

describe('SupplyListDetailState', () => {
  let http: {
    getById: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
  };

  const request: SaveSupplyListRequest = {
    name: 'Lista',
    frequency: 'Weekly',
    intervalWeeks: null,
    note: null,
    lines: [{ itemId: 'item-1', quantity: 1, unit: 'buc', notes: null }]
  };

  beforeEach(() => {
    http = { getById: vi.fn(), create: vi.fn(), update: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideDispatcher(),
        SupplyListDetailState,
        { provide: SupplyListsHttp, useValue: http }
      ]
    });
  });

  it('loads a supply list by id', () => {
    http.getById.mockReturnValue(of(createDto()));
    const store = TestBed.inject(SupplyListDetailState);

    store.loadSupplyList('list-1');

    expect(http.getById).toHaveBeenCalledWith('list-1');
    expect(store.supplyList().name).toBe('Lista');
    expect(store.supplyListLoading()).toBe(false);
  });

  it('creates a new list and dispatches save success', () => {
    http.create.mockReturnValue(of(createDto()));
    const store = TestBed.inject(SupplyListDetailState);
    const events = TestBed.inject(Events);
    const success = vi.fn();
    events.on(supplyListApiEvents.saveSuccess).subscribe(({ payload }) => success(payload.operationId));

    store.saveSupplyList({ request, operationId: 'op-1' });

    expect(http.create).toHaveBeenCalledWith(request);
    expect(success).toHaveBeenCalledWith('op-1');
    expect(store.supplyList().id).toBe('list-1');
  });

  it('updates an existing list', () => {
    http.getById.mockReturnValue(of(createDto()));
    http.update.mockReturnValue(of(createDto()));
    const store = TestBed.inject(SupplyListDetailState);

    store.loadSupplyList('list-1');
    store.saveSupplyList({ request, operationId: 'op-1' });

    expect(http.update).toHaveBeenCalledWith('list-1', request);
    expect(http.create).not.toHaveBeenCalled();
  });

  it('dispatches save failure and exposes the problem detail', () => {
    http.create.mockReturnValue(throwError(() => ({ status: 400, title: 'Validation' })));
    const store = TestBed.inject(SupplyListDetailState);
    const events = TestBed.inject(Events);
    const failure = vi.fn();
    events.on(supplyListApiEvents.saveFailure).subscribe(({ payload }) => failure(payload.operationId));

    store.saveSupplyList({ request, operationId: 'op-2' });

    expect(failure).toHaveBeenCalledWith('op-2');
    expect(store.supplyListProblemDetail()).toEqual({ status: 400, title: 'Validation' });
  });
});

function createDto(): SupplyListDto {
  return {
    id: 'list-1',
    name: 'Lista',
    note: null,
    frequency: 'Weekly',
    intervalWeeks: null,
    isActive: true,
    lines: [],
    createdDate: '2025-01-01T00:00:00Z',
    lastModifiedDate: '2025-01-01T00:00:00Z'
  };
}
