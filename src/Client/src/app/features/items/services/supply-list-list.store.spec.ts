import { TestBed } from '@angular/core/testing';
import { Dispatcher, provideDispatcher } from '@ngrx/signals/events';
import { type SupplyListDto, type SupplyListListItemDto } from '@ske/models';
import { SupplyListsHttp } from '@ske/shared/supply-lists';
import { realtimeEvents } from '@ske/signalr';
import { NzMessageService } from 'ng-zorro-antd/message';
import { of, throwError } from 'rxjs';
import { SupplyListListState } from './supply-list-list.store';

describe('SupplyListListState', () => {
  let http: {
    getAll: ReturnType<typeof vi.fn>;
    disable: ReturnType<typeof vi.fn>;
    enable: ReturnType<typeof vi.fn>;
  };
  let messageService: { success: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    http = { getAll: vi.fn(), disable: vi.fn(), enable: vi.fn() };
    messageService = { success: vi.fn() };
    http.getAll.mockReturnValue(of({ data: [createListItem()], hasNextPage: false, nextCursor: null, sort: [] }));

    TestBed.configureTestingModule({
      providers: [
        provideDispatcher(),
        SupplyListListState,
        { provide: SupplyListsHttp, useValue: http },
        { provide: NzMessageService, useValue: messageService }
      ]
    });
  });

  it('defaults to the active-only filter', () => {
    const store = TestBed.inject(SupplyListListState);

    expect(store.filter().filters).toEqual([
      expect.objectContaining({ field: 'isActive', operator: 'equals', value: true })
    ]);
  });

  it('loads supply lists and resets the cursor', () => {
    const store = TestBed.inject(SupplyListListState);

    store.load({ ...store.filter(), searchTerm: 'rechizite' });

    expect(http.getAll).toHaveBeenCalledWith(expect.objectContaining({ searchTerm: 'rechizite', cursor: null }));
    expect(store.supplyLists()).toHaveLength(1);
    expect(store.supplyListsLoading()).toBe(false);
  });

  it('appends the next page when loading more', () => {
    http.getAll
      .mockReturnValueOnce(of({ data: [createListItem('1')], hasNextPage: true, nextCursor: 'c1', sort: [] }))
      .mockReturnValueOnce(of({ data: [createListItem('2')], hasNextPage: false, nextCursor: null, sort: [] }));
    const store = TestBed.inject(SupplyListListState);

    store.load(store.filter());
    store.loadMore();

    expect(http.getAll).toHaveBeenLastCalledWith(expect.objectContaining({ cursor: 'c1' }));
    expect(store.supplyLists().map((x) => x.id)).toEqual(['1', '2']);
    expect(store.isLoadingMore()).toBe(false);
  });

  it('disables an active list, then reloads', () => {
    http.disable.mockReturnValue(of({ ...createDto(), isActive: false }));
    const store = TestBed.inject(SupplyListListState);

    store.toggleActive(createListItem('1', true));

    expect(http.disable).toHaveBeenCalledWith('1');
    expect(http.getAll).toHaveBeenCalled();
    expect(store.togglingId()).toBeNull();
    expect(messageService.success).toHaveBeenCalledWith('Lista a fost dezactivată.');
  });

  it('enables an inactive list', () => {
    http.enable.mockReturnValue(of(createDto()));
    const store = TestBed.inject(SupplyListListState);

    store.toggleActive(createListItem('1', false));

    expect(http.enable).toHaveBeenCalledWith('1');
    expect(messageService.success).toHaveBeenCalledWith('Lista a fost activată.');
  });

  it('exposes toggle failures and clears the pending row', () => {
    http.disable.mockReturnValue(throwError(() => ({ status: 404, title: 'Not found' })));
    const store = TestBed.inject(SupplyListListState);

    store.toggleActive(createListItem('1', true));

    expect(store.togglingId()).toBeNull();
    expect(store.supplyListsProblemDetail()).toEqual({ status: 404, title: 'Not found' });
  });

  it('reloads when a realtime supply list event arrives', () => {
    TestBed.inject(SupplyListListState);
    const dispatcher = TestBed.inject(Dispatcher);

    dispatcher.dispatch(realtimeEvents.supplyListUpdated({ supplyListId: '1' }));

    expect(http.getAll).toHaveBeenCalledTimes(1);
  });
});

function createListItem(id = '1', isActive = true): SupplyListListItemDto {
  return {
    id,
    name: `Lista ${id}`,
    frequency: 'Weekly',
    intervalWeeks: null,
    isActive,
    lineCount: 2,
    createdDate: '2025-01-01T00:00:00Z',
    lastModifiedDate: '2025-01-01T00:00:00Z'
  };
}

function createDto(): SupplyListDto {
  return {
    id: '1',
    name: 'Lista 1',
    note: null,
    frequency: 'Weekly',
    intervalWeeks: null,
    isActive: true,
    lines: [],
    createdDate: '2025-01-01T00:00:00Z',
    lastModifiedDate: '2025-01-01T00:00:00Z'
  };
}
