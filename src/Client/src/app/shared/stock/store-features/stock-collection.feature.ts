import { GetClassLocationStockRequest, StockItemDto } from '@ske/models';
import { patchState, signalStoreFeature, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { StockHttp } from '../services/stock.http';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';

type StockCollectionState = {
  stockItems: StockItemDto[];
  filter: GetClassLocationStockRequest;
};

const initialState: StockCollectionState = {
  stockItems: [],
  filter: {
    classId: 0,
    locationId: null,
    searchTerm: null
  }
};

/**
 * Loads the current stock report for a class (optionally scoped to a location) in a single
 * request - unlike `withStockBatchCollection`/`withGoodReceiptsFeature`, there is no keyset
 * cursor/`loadMore` here since `GetClassLocationStockQuery` returns the full report at once.
 */
export function withStockCollection() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('stockItems'),
    withProblemDetailsFeature('stockItems'),
    withProps(() => ({
      stockHttp: inject(StockHttp)
    })),
    withMethods((store) => {
      const load = rxMethod<GetClassLocationStockRequest>(
        pipe(
          tap((request) => {
            store.setStockItemsLoading();
            store.clearStockItemsErrors();
            patchState(store, { filter: request });
          }),
          switchMap((request) =>
            store.stockHttp.getClassLocationStock(request)
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, { stockItems: result });
                    store.setStockItemsLoaded();
                  },
                  error: (error) => {
                    store.handleStockItemsError(error);
                    store.setStockItemsLoaded();
                  }
                })
              )
          )
        )
      );

      return { load };
    })
  );
}
