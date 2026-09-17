import {
  GetClassLocationStockRequest,
  RemoveExpiredStockRequest,
  SetClassItemStockVisibilityRequest,
  StockItemCategoryGroup,
  StockItemDto
} from '@ske/models';
import { patchState, signalStoreFeature, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { StockHttp } from './stock.http';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { map, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { realtimeEvents } from '@ske/signalr';
import { StockPreferencesService } from './stock-preferences.service';
import { NzMessageService } from 'ng-zorro-antd/message';

type StockCollectionState = {
  stockItems: StockItemDto[];
  hasExpiredItems: boolean;
  filter: GetClassLocationStockRequest;
  groupedStockItems: Map<StockItemCategoryGroup, StockItemDto[]>;
};

const initialState: StockCollectionState = {
  stockItems: [],
  hasExpiredItems: false,
  filter: {
    classId: '',
    filters: [],
    searchTerm: null,
    includeHidden: false,
    lowStockOnly: false,
    expiredOnly: false
  },
  groupedStockItems: new Map()
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
    withLoadingFeature('stockMutation'),
    withProblemDetailsFeature('stockMutation'),
    withProps(() => ({
      stockHttp: inject(StockHttp),
      stockPreferences: inject(StockPreferencesService),
      messageService: inject(NzMessageService)
    })),
    withMethods((store) => {
      const load = rxMethod<GetClassLocationStockRequest>(
        pipe(
          tap((request) => {
            store.setStockItemsLoading();
            store.clearStockItemsErrors();
            store.stockPreferences.setShowHiddenProducts(request.includeHidden);
            patchState(store, {filter: request});
          }),
          switchMap((request) =>
            store.stockHttp.getClassLocationStock(request)
              .pipe(
                mapResponse({
                  next: (result) => {
                    const groupedStockItems = new Map<StockItemCategoryGroup, StockItemDto[]>();
                    const groupsByCategoryId = new Map<string, StockItemCategoryGroup>();

                    for (const item of result.items) {
                      let group = groupsByCategoryId.get(item.categoryId);
                      if (!group) {
                        group = {
                          categoryId: item.categoryId,
                          categoryName: item.categoryName,
                          categoryIcon: item.categoryIcon ?? null
                        };
                        groupsByCategoryId.set(item.categoryId, group);
                        groupedStockItems.set(group, []);
                      }
                      groupedStockItems.get(group)!.push(item);
                    }

                    patchState(store, {
                      stockItems: result.items,
                      hasExpiredItems: result.hasExpiredItems,
                      groupedStockItems
                    });
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

      const removeExpiredStock = rxMethod<{
        request: RemoveExpiredStockRequest;
        expiredQuantity: number;
      }>(
        pipe(
          tap(() => {
            store.setStockMutationLoading();
            store.clearStockMutationErrors();
          }),
          switchMap(({ request, expiredQuantity }) =>
            store.stockHttp.removeExpiredStock(request).pipe(
              mapResponse({
                next: () => {
                  store.messageService.success(`Au fost eliminate ${expiredQuantity} articole expirate.`);
                  store.setStockMutationLoaded();
                  load(store.filter());
                },
                error: (error) => {
                  store.handleStockMutationError(error);
                  store.messageService.error('Articolele expirate nu au putut fi eliminate.');
                  store.setStockMutationLoaded();
                }
              })
            )
          )
        )
      );

      const setClassItemStockVisibility = rxMethod<{
        classId: string;
        itemId: string;
        request: SetClassItemStockVisibilityRequest;
      }>(
        pipe(
          tap(() => {
            store.setStockMutationLoading();
            store.clearStockMutationErrors();
          }),
          switchMap(({ classId, itemId, request }) =>
            store.stockHttp.setClassItemStockVisibility(classId, itemId, request).pipe(
              mapResponse({
                next: () => {
                  store.messageService.success(
                    request.hideWhenZeroStock
                      ? 'Produsul va fi ascuns când stocul ajunge la 0.'
                      : 'Produsul nu va mai fi ascuns la stoc 0.'
                  );
                  store.setStockMutationLoaded();
                  load(store.filter());
                },
                error: (error) => {
                  store.handleStockMutationError(error);
                  store.messageService.error('Setarea vizibilității produsului nu a putut fi salvată.');
                  store.setStockMutationLoaded();
                }
              })
            )
          )
        )
      );

      return { load, removeExpiredStock, setClassItemStockVisibility };
    }),
    withEventHandlers((store, events = inject(Events)) => ({
      stockChanged: events.on(
        realtimeEvents.goodsReceiptImportConfirmed,
        realtimeEvents.stockAdjusted,
        realtimeEvents.stockMoved,
        realtimeEvents.stockBatchCreated,
        realtimeEvents.classItemStockVisibilityChanged
      )
        .pipe(
          map(() => store.filter()),
          tap((filter) => store.load(filter))
        )
    }))
  );
}
