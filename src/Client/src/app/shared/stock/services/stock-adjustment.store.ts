import { AdjustStockRequest, StockItemDto } from '@ske/models';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { StockHttp } from './stock.http';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';

type StockAdjustmentState = { adjustedItem: StockItemDto | null };
const initialState: StockAdjustmentState = { adjustedItem: null };

export const stockApiEvents = eventGroup({
  source: 'Stock API',
  events: {
    adjustSuccess: type<void>()
  }
});

/**
 * Mutation-only counterpart to `withStockCollection` - reconciles a physical recount for a
 * single item/location/class via `POST /api/Stock/adjust` (see AdjustStockCommand).
 */
export const StockAdjustmentState = signalStore(
  withState(initialState),
  withLoadingFeature('stockAdjustment'),
  withProblemDetailsFeature('stockAdjustment'),
  withProps(() => ({
    stockHttp: inject(StockHttp),
    dispatcher: injectDispatch(stockApiEvents)
  })),
  withMethods((store) => {
    const adjustStock = rxMethod<AdjustStockRequest>(
      pipe(
        tap(() => {
          store.setStockAdjustmentLoading();
          store.clearStockAdjustmentErrors();
        }),
        switchMap((request) =>
          store.stockHttp.adjustStock(request).pipe(
            mapResponse({
              next: (adjustedItem) => {
                patchState(store, { adjustedItem });
                store.dispatcher.adjustSuccess();
                store.setStockAdjustmentLoaded();
              },
              error: (error) => {
                store.handleStockAdjustmentError(error);
                store.setStockAdjustmentLoaded();
              }
            })
          )
        )
      )
    );

    return { adjustStock };
  })
);
