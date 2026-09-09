import {patchState, signalStoreFeature, withComputed, withMethods, withState} from '@ngrx/signals';
import {withLoadingFeature} from '@ske/shared/loader';
import {withProblemDetailsFeature} from '@ske/shared/errors';
// noinspection ES6PreferShortImport
import {GoodsReceiptsHttp} from '../services/goods-receipts.http';
import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {EMPTY, filter, map, pipe, switchMap, tap} from 'rxjs';
import {
  buildGoodsReceiptListFilter,
  GetAllGoodsReceiptsRequest,
  GoodsReceiptListItemDto,
  PaginatedResponseData
} from '@ske/models';
import {mapResponse} from '@ngrx/operators';
import {Events, withEventHandlers} from '@ngrx/signals/events';
import {realtimeEvents} from '@ske/signalr';

type GoodsReceiptsState = {
  goodsReceipts: GoodsReceiptListItemDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllGoodsReceiptsRequest;
  isLoadingMoreGoodsReceipts: boolean;
};
const initialState: GoodsReceiptsState = {
  goodsReceipts: [],
  paginationData: null,
  filter: {
    filters: [],
    sort: []
  },
  isLoadingMoreGoodsReceipts: false
};

export function withGoodReceiptsFeature() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('goodsReceipts'),
    withProblemDetailsFeature('goodsReceipts'),
    withComputed((store) => ({
      hasGoodsReceiptsNextPage: () => store.paginationData()?.hasNextPage ?? false,
      nextGoodReceiptsCursor: () => store.paginationData()?.nextCursor ?? null
    })),
    withMethods((store) => {
      const goodsReceiptsHttp = inject(GoodsReceiptsHttp);

      const loadGoodsReceipts = rxMethod<GetAllGoodsReceiptsRequest>(
        pipe(
          tap((filter) => {
            store.clearGoodsReceiptsErrors();
            store.goodsReceiptsLoading();

            patchState(store, {filter, isLoadingMoreGoodsReceipts: false});
          }),
          map((filter) => buildGoodsReceiptListFilter(store.filter(), filter)),
          switchMap((filter) => {

            return goodsReceiptsHttp.getAll(filter)
              .pipe(
                mapResponse({
                  next: result => {
                    patchState(store, {
                      goodsReceipts: result.data,
                      paginationData: result,
                      filter: {...filter, cursor: null, sort: result.sort}
                    });
                  },
                  error: error => {
                    store.handleGoodsReceiptsError(error);
                    store.setGoodsReceiptsLoaded();
                  }
                })
              );
          })
        )
      );

      const loadMoreGoodsReceipts = rxMethod<void>(
        pipe(
          filter(() => store.hasGoodsReceiptsNextPage() && !store.isLoadingMoreGoodsReceipts()),
          tap(() => {
            store.clearGoodsReceiptsErrors();
            patchState(store, {isLoadingMoreGoodsReceipts: true});
          }),
          switchMap(() => {
            const filter = store.filter();
            const nextCursor = store.nextGoodReceiptsCursor();

            if (!nextCursor) {
              patchState(store, {isLoadingMoreGoodsReceipts: false});
              return EMPTY;
            }

            return goodsReceiptsHttp.getAll({...filter, cursor: nextCursor})
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      goodsReceipts: [...store.goodsReceipts(), ...result.data],
                      paginationData: result,
                      filter: {...filter, cursor: null, sort: result.sort},
                      isLoadingMoreGoodsReceipts: false
                    });
                    store.setGoodsReceiptsLoaded();
                  },
                  error: (error) => {
                    store.handleGoodsReceiptsError(error);
                    patchState(store, {isLoadingMoreGoodsReceipts: false});
                  }
                })
              );

          })
        )
      );

      return {loadGoodsReceipts, loadMoreGoodsReceipts};
    }),
    withEventHandlers((store, events = inject(Events)) => ({
      goodsReceiptImportChanges: events.on(realtimeEvents.goodsReceiptImportConfirmed)
        .pipe(
          map(() => store.filter()),
          filter((filter): filter is GetAllGoodsReceiptsRequest => !!filter),
          tap((filter) => store.loadGoodsReceipts(filter))
        )
    }))
  );
}
