import { signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { CreateStockBatchRequest } from '@ske/models';
import { pipe, switchMap, tap } from 'rxjs';
import { StockBatchesHttp } from '@ske/shared/stock-batches';
import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';

type StockBatchState = {};
const initialState: StockBatchState = {};
export const stockBatchApiEvents = eventGroup({
  source: 'Stock Batch API',
  events: {
    saveSuccess: type<void>()
  }
});
export const StockBatchStore = signalStore(
  withState(initialState),
  withLoadingFeature('stockBatch'),
  withProblemDetailsFeature('stockBatch'),
  withProps(() => ({
    stockBatchHttp: inject(StockBatchesHttp),
    dispatcher: injectDispatch(stockBatchApiEvents)
  })),
  withMethods((store) => {
    const save = rxMethod<CreateStockBatchRequest>(
      pipe(
        tap(() => {
          store.clearStockBatchErrors();
          store.setStockBatchLoading();
        }),
        switchMap(request =>
          store.stockBatchHttp.create(request)
            .pipe(
              mapResponse({
                next: (result) => {
                  store.setStockBatchLoaded();
                  store.dispatcher.saveSuccess();
                },
                error: (error) => {
                  store.handleStockBatchError(error);
                  store.setStockBatchLoaded();
                }
              })
            )
        )
      )
    );

    return { save };
  })
);
