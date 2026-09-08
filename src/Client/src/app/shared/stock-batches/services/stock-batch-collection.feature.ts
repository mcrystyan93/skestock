import {
  buildStockBatchListFilter,
  GetAllStockBatchesRequest,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData,
  StockBatchListItemDto
} from '@ske/models';
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
// noinspection ES6PreferShortImport
import { StockBatchesHttp } from './stock-batches.http';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, filter, map, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';

type StockBatchCollectionState = {
  stockBatches: StockBatchListItemDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllStockBatchesRequest;
  isLoadingMore: boolean;
};

const initialState: StockBatchCollectionState = {
  stockBatches: [],
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

export function withStockBatchCollection() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('stockBatches'),
    withProblemDetailsFeature('stockBatches'),
    withComputed((store) => ({
      hasNextPage: () => store.paginationData()?.hasNextPage ?? false,
      nextCursor: () => store.paginationData()?.nextCursor ?? null
    })),
    withProps(() => ({
      stockBatchesHttp: inject(StockBatchesHttp)
    })),
    withMethods((store) => {
      const load = rxMethod<GetAllStockBatchesRequest>(
        pipe(
          map((data) => buildStockBatchListFilter(store.filter(), data)),
          tap((filter) => {
            store.clearStockBatchesErrors();
            store.setStockBatchesLoading();

            patchState(store, { filter, isLoadingMore: false });
          }),
          switchMap((filter) =>
            store.stockBatchesHttp.getAll(filter)
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      stockBatches: result.data,
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort }
                    });
                    store.setStockBatchesLoaded();
                  },
                  error: (error) => {
                    store.handleStockBatchesError(error);
                    store.setStockBatchesLoaded();
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
            store.clearStockBatchesErrors();
            patchState(store, { isLoadingMore: true });
          }),
          switchMap(() => {
            const filter = store.filter();
            const nextCursor = store.nextCursor();

            if (!nextCursor) {
              patchState(store, { isLoadingMore: false });
              return EMPTY;
            }

            return store.stockBatchesHttp.getAll({ ...filter, cursor: nextCursor })
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      stockBatches: [...store.stockBatches(), ...result.data],
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort },
                      isLoadingMore: false
                    });
                    store.setStockBatchesLoaded();
                  },
                  error: (error) => {
                    store.handleStockBatchesError(error);
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
