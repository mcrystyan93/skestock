import {
  CongregationDto,
  GetAllCongregationsRequest,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData, buildCongregationListFilter
} from '@ske/models';
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
// noinspection ES6PreferShortImport
import { CongregationsHttp } from './congregations.http';
import { realtimeEvents } from '@ske/signalr';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, filter, map, merge, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { Events, withEventHandlers } from '@ngrx/signals/events';


type CongregationCollectionState = {
  congregations: CongregationDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllCongregationsRequest;
  isLoadingMore: boolean;
};

const initialState: CongregationCollectionState = {
  congregations: [],
  paginationData: null,
  filter: {
    sort: [
      {
        key: 'name',
        value: 'ascend'
      }
    ],
    filters: [],
    cursor: null,
    pageSize: PAGINATION_PAGE_SIZE,
    searchTerm: null
  },
  isLoadingMore: false
};

export function withCongregationCollection() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('congregations'),
    withProblemDetailsFeature('congregations'),
    withComputed((store) => ({
      hasNextPage: () => store.paginationData()?.hasNextPage ?? false,
      nextCursor: () => store.paginationData()?.nextCursor ?? null
    })),
    withProps(() => ({
      congregationHttp: inject(CongregationsHttp)
    })),
    withMethods((store) => {
      const load = rxMethod<GetAllCongregationsRequest>(
        pipe(
          map(data => buildCongregationListFilter(store.filter(), data)),
          tap((filter) => {
            store.clearCongregationsErrors();
            store.setCongregationsLoading();

            patchState(store, { filter, isLoadingMore: false });
          }),
          switchMap(filter =>
            store.congregationHttp.getAll(filter)
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      congregations: result.data,
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort }
                    });
                    store.setCongregationsLoaded();
                  },
                  error: (error) => {
                    store.handleCongregationsError(error);
                    store.setCongregationsLoaded();
                  }
                })
              )
          )
        )
      );

      const loadMore = rxMethod<void>(
        pipe(
          filter(() => store.hasNextPage() && !store.isLoadingMore()),
          tap(() => {
            store.clearCongregationsErrors();
            patchState(store, { isLoadingMore: true });
          }),
          switchMap(() => {
            const filter = store.filter();
            const nextCursor = store.nextCursor();

            if (!nextCursor) {
              patchState(store, { isLoadingMore: false });
              return EMPTY;
            }

            return store.congregationHttp.getAll({ ...filter, cursor: nextCursor })
              .pipe(
                mapResponse({
                  next: (result) => {
                    // A newer load() replaced the filter while this page was in flight; its rows are stale.
                    if (store.filter() !== filter) return;

                    patchState(store, {
                      congregations: [...store.congregations(), ...result.data],
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort },
                      isLoadingMore: false
                    });
                    store.setCongregationsLoaded();
                  },
                  error: (error) => {
                    if (store.filter() !== filter) return;

                    store.handleCongregationsError(error);
                    patchState(store, { isLoadingMore: false });
                  }
                })
              );

          })
        )
      );

      return { load, loadMore };
    }),
    withEventHandlers((store, events = inject(Events)) => ({
      // Any change can affect the current filter/sort, so refetch the first page. Stores that never
      // loaded (e.g. a dropdown that was not opened) have nothing to refresh.
      congregationChanged: merge(
        events.on(realtimeEvents.congregationCreated),
        events.on(realtimeEvents.congregationUpdated),
        events.on(realtimeEvents.congregationDeleted)
      ).pipe(
        filter(() => store.paginationData() !== null),
        tap(() => store.load(store.filter()))
      )
    }))
  );
}
