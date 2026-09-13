import { inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { EMPTY, concatMap, filter, map, pipe, switchMap, tap } from 'rxjs';
import {
  buildItemImportBatchListFilter,
  CreateItemImportBatchRequest,
  ItemImportBatchDto,
  GetAllItemImportBatchesRequest,
  ItemImportBatchListItemDto,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData
} from '@ske/models';
import { realtimeEvents } from '@ske/signalr';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ItemImportsHttp } from './item-imports.http';

type ItemImportStateModel = {
  itemImportBatch: ItemImportBatchDto | null;
  itemImportListItems: ItemImportBatchListItemDto[];
  itemImportListPaginationData: PaginatedResponseData | null;
  itemImportListFilter: GetAllItemImportBatchesRequest;
  itemImportListLoadingMore: boolean;
};

const initialState: ItemImportStateModel = {
  itemImportBatch: null,
  itemImportListItems: [],
  itemImportListPaginationData: null,
  itemImportListFilter: {
    sort: [],
    filters: [],
    cursor: null,
    pageSize: PAGINATION_PAGE_SIZE,
    searchTerm: null
  },
  itemImportListLoadingMore: false
};

export const ItemImportState = signalStore(
  withState(initialState),
  withLoadingFeature('itemImport'),
  withLoadingFeature('itemImportList'),
  withProblemDetailsFeature('itemImport'),
  withProblemDetailsFeature('itemImportList'),
  withProps(() => ({ itemImportsHttp: inject(ItemImportsHttp) })),
  withComputed((store) => ({
    itemImportListHasNextPage: () => store.itemImportListPaginationData()?.hasNextPage ?? false,
    itemImportListNextCursor: () => store.itemImportListPaginationData()?.nextCursor ?? null
  })),
  withMethods((store) => {
    const createBatch = rxMethod<CreateItemImportBatchRequest>(
      pipe(
        tap(() => {
          store.clearItemImportErrors();
          store.setItemImportLoading();
          patchState(store, { itemImportBatch: null });
        }),
        concatMap((request) => store.itemImportsHttp.createBatch(request)),
        mapResponse({
          next: (itemImportBatch) => {
            patchState(store, { itemImportBatch });
            store.setItemImportLoaded();
          },
          error: (error) => {
            store.handleItemImportError(error);
            store.setItemImportLoaded();
          }
        })
      )
    );

    const load = rxMethod<GetAllItemImportBatchesRequest>(
      pipe(
        map((data) => buildItemImportBatchListFilter(store.itemImportListFilter(), data)),
        tap((filter) => {
          store.clearItemImportListErrors();
          store.setItemImportListLoading();
          patchState(store, { itemImportListFilter: filter, itemImportListLoadingMore: false });
        }),
        switchMap((filter) => store.itemImportsHttp.getAll(filter).pipe(
          mapResponse({
            next: (result) => {
              patchState(store, {
                itemImportListItems: result.data,
                itemImportListPaginationData: result,
                itemImportListFilter: { ...filter, cursor: null, sort: result.sort }
              });
              store.setItemImportListLoaded();
            },
            error: (error) => {
              store.handleItemImportListError(error);
              store.setItemImportListLoaded();
            }
          })
        ))
      )
    );

    const loadMore = rxMethod<void>(
      pipe(
        filter(() => store.itemImportListHasNextPage() && !store.itemImportListLoadingMore()),
        tap(() => {
          store.clearItemImportListErrors();
          patchState(store, { itemImportListLoadingMore: true });
        }),
        switchMap(() => {
          const currentFilter = store.itemImportListFilter();
          const nextCursor = store.itemImportListNextCursor();

          if (!nextCursor) {
            patchState(store, { itemImportListLoadingMore: false });
            return EMPTY;
          }

          return store.itemImportsHttp.getAll({ ...currentFilter, cursor: nextCursor }).pipe(
            mapResponse({
              next: (result) => {
                patchState(store, {
                  itemImportListItems: [...store.itemImportListItems(), ...result.data],
                  itemImportListPaginationData: result,
                  itemImportListFilter: { ...currentFilter, cursor: null, sort: result.sort },
                  itemImportListLoadingMore: false
                });
                store.setItemImportListLoaded();
              },
              error: (error) => {
                store.handleItemImportListError(error);
                patchState(store, { itemImportListLoadingMore: false });
              }
            })
          );
        })
      )
    );

    const reload = () => load(store.itemImportListFilter());

    return { createBatch, load, loadMore, reload };
  }),
  withEventHandlers((store, events = inject(Events)) => ({
    itemImportChanges: events.on(
      realtimeEvents.itemImportBatchCreated,
      realtimeEvents.itemImportBatchProcessed,
      realtimeEvents.itemImportBatchConfirmed
    ).pipe(tap(() => store.reload()))
  }))
);
