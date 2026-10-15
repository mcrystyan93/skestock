import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { pipe, switchMap } from 'rxjs';
import { GoodsReceiptImportLineDto } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { GoodsReceiptsHttp } from './goods-receipts.http';

export const GoodsReceiptImportLinesStore = signalStore(
  withState({ receiptId: null as string | null, lines: [] as GoodsReceiptImportLineDto[] }),
  withLoadingFeature('lines'),
  withProblemDetailsFeature('lines'),
  withMethods((store, http = inject(GoodsReceiptsHttp)) => {
    const load = rxMethod<string>(pipe(
      switchMap(receiptId => {
        patchState(store, { receiptId, lines: [] });
        store.clearLinesErrors();
        store.setLinesLoading();
        return http.getImportLines(receiptId).pipe(mapResponse({
          next: lines => {
            patchState(store, { lines });
            store.setLinesLoaded();
          },
          error: error => {
            store.handleLinesError(error);
            store.setLinesLoaded();
          }
        }));
      })
    ));

    return {
      load,
      retry() {
        const receiptId = store.receiptId();
        if (receiptId) load(receiptId);
      }
    };
  })
);
