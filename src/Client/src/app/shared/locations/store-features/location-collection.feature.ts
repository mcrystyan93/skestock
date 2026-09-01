import {
  LocationDto,
  GetAllLocationsRequest,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData, buildLocationListFilter
} from '@ske/models';
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { LocationsHttp } from '@ske/shared/locations';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, filter, map, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';


type LocationCollectionState = {
  locations: LocationDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllLocationsRequest;
  isLoadingMore: boolean;
};

const initialState: LocationCollectionState = {
  locations: [],
  paginationData: null,
  filter: {
    sort: [],
    filters: [],
    cursor: null,
    pageSize: PAGINATION_PAGE_SIZE,
    searchTerm: null
  },
  isLoadingMore: false
};

export function withLocationCollection() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('locations'),
    withProblemDetailsFeature('locations'),
    withComputed((store) => ({
      hasNextPage: () => store.paginationData()?.hasNextPage ?? false,
      nextCursor: () => store.paginationData()?.nextCursor ?? null
    })),
    withProps(() => ({
      locationHttp: inject(LocationsHttp)
    })),
    withMethods((store) => {
      const load = rxMethod<GetAllLocationsRequest>(
        pipe(
          map(data => buildLocationListFilter(store.filter(), data)),
          tap((filter) => {
            store.clearLocationsErrors();
            store.locationsLoading();

            patchState(store, { filter, isLoadingMore: false });
          }),
          switchMap(filter =>
            store.locationHttp.getAll(filter)
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      locations: result.data,
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort }
                    });
                    store.setLocationsLoaded();
                  },
                  error: (error) => {
                    store.handleLocationsError(error);
                    store.setLocationsLoaded();
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
            store.clearLocationsErrors();
            patchState(store, { isLoadingMore: true });
          }),
          switchMap(() => {
            const filter = store.filter();
            const nextCursor = store.nextCursor();

            if (!nextCursor) {
              patchState(store, { isLoadingMore: false });
              return EMPTY;
            }

            return store.locationHttp.getAll({ ...filter, cursor: nextCursor })
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      locations: [...store.locations(), ...result.data],
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort },
                      isLoadingMore: false
                    });
                    store.setLocationsLoaded();
                  },
                  error: (error) => {
                    store.handleLocationsError(error);
                    patchState(store, { isLoadingMore: false });
                  }
                })
              );

          })
        )
      );

      return { load, loadMore };
    })
  );
}
