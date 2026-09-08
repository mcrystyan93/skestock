import {
  buildGoodsReceiptImportListFilter,
  GetAllGoodsReceiptImportsRequest,
  GoodsReceiptImportListItemDto,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData
} from '@ske/models';
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
// noinspection ES6PreferShortImport
import { GoodsReceiptImportsHttp } from './goods-receipt-imports.http';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, filter, map, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { Events, on, withEventHandlers, withReducer } from '@ngrx/signals/events';
import { goodsReceiptImportRealtimeEvents } from './goods-receipt-import.events';
import { NzMessageService } from 'ng-zorro-antd/message';

type GoodsReceiptImportCollectionState = {
  goodsReceiptImports: GoodsReceiptImportListItemDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllGoodsReceiptImportsRequest;
  isLoadingMore: boolean;
  listHasChanged: boolean;
};

const initialState: GoodsReceiptImportCollectionState = {
  goodsReceiptImports: [],
  paginationData: null,
  filter: {
    sort: [],
    filters: [],
    cursor: null,
    pageSize: PAGINATION_PAGE_SIZE,
    searchTerm: null
  },
  isLoadingMore: false,
  listHasChanged: false
};

export function withGoodsReceiptImportCollection() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('goodsReceiptImports'),
    withProblemDetailsFeature('goodsReceiptImports'),
    withComputed((store) => ({
      hasNextPage: () => store.paginationData()?.hasNextPage ?? false,
      nextCursor: () => store.paginationData()?.nextCursor ?? null
    })),
    withProps(() => ({
      goodsReceiptImportsHttp: inject(GoodsReceiptImportsHttp)
    })),
    withMethods((store) => {
      const load = rxMethod<GetAllGoodsReceiptImportsRequest>(
        pipe(
          map((data) => buildGoodsReceiptImportListFilter(store.filter(), data)),
          tap((filter) => {
            store.clearGoodsReceiptImportsErrors();
            store.setGoodsReceiptImportsLoading();

            patchState(store, { filter, isLoadingMore: false });
          }),
          switchMap((filter) =>
            store.goodsReceiptImportsHttp.getAll(filter)
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      goodsReceiptImports: result.data,
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort }
                    });
                    store.setGoodsReceiptImportsLoaded();
                  },
                  error: (error) => {
                    store.handleGoodsReceiptImportsError(error);
                    store.setGoodsReceiptImportsLoaded();
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
            store.clearGoodsReceiptImportsErrors();
            patchState(store, { isLoadingMore: true });
          }),
          switchMap(() => {
            const filter = store.filter();
            const nextCursor = store.nextCursor();

            if (!nextCursor) {
              patchState(store, { isLoadingMore: false });
              return EMPTY;
            }

            return store.goodsReceiptImportsHttp.getAll({ ...filter, cursor: nextCursor })
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      goodsReceiptImports: [...store.goodsReceiptImports(), ...result.data],
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort },
                      isLoadingMore: false
                    });
                    store.setGoodsReceiptImportsLoaded();
                  },
                  error: (error) => {
                    store.handleGoodsReceiptImportsError(error);
                    patchState(store, { isLoadingMore: false });
                  }
                })
              );
          })
        )
      );

      return { load, loadMore };
    }),
    withEventHandlers((store, events = inject(Events), nzMessageService = inject(NzMessageService)) => ({
      notifyUser: events.on(goodsReceiptImportRealtimeEvents.goodsReceiptImportProcessed)
        .pipe(
          tap(() => nzMessageService.success(
            'Importul de bunuri a fost procesat cu succes. Lista a fost reîncărcată.',
            { nzDuration: 5000 })
          )
        ),
      importCreated: events.on(goodsReceiptImportRealtimeEvents.goodsReceiptImportCreated, goodsReceiptImportRealtimeEvents.goodsReceiptImportProcessed)
        .pipe(
          tap(() => console.log('signalR event from goods-receipt-import-collection')),
          tap(() => store.load(store.filter()))
        )
    }))
  );
}
