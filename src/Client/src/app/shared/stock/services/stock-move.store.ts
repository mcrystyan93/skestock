import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { MoveStockRequest } from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { StockHttp } from './stock.http';

type StockMoveState = {
  completed: boolean;
};

const initialState: StockMoveState = {
  completed: false
};

export const stockMoveApiEvents = eventGroup({
  source: 'Stock API',
  events: {
    moveSuccess: type<void>()
  }
});

export const StockMoveState = signalStore(
  withState(initialState),
  withLoadingFeature('stockMove'),
  withProblemDetailsFeature('stockMove'),
  withProps(() => ({
    stockHttp: inject(StockHttp),
    dispatcher: injectDispatch(stockMoveApiEvents)
  })),
  withMethods((store) => {
    const moveStock = rxMethod<MoveStockRequest>(
      pipe(
        tap(() => {
          store.setStockMoveLoading();
          store.clearStockMoveErrors();
          patchState(store, { completed: false });
        }),
        switchMap((request) =>
          store.stockHttp.moveStock(request).pipe(
            mapResponse({
              next: () => {
                patchState(store, { completed: true });
                store.dispatcher.moveSuccess();
                store.setStockMoveLoaded();
              },
              error: (error) => {
                store.handleStockMoveError(error);
                store.setStockMoveLoaded();
              }
            })
          )
        )
      )
    );

    return { moveStock };
  })
);
