import { GetClassLocationStockRequest, StockItemCategoryGroup, StockItemDto } from '@ske/models';
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

type StockCollectionState = {
  stockItems: StockItemDto[];
  filter: GetClassLocationStockRequest;
  groupedStockItems: Map<StockItemCategoryGroup, StockItemDto[]>;
};

const initialState: StockCollectionState = {
  stockItems: [],
  filter: {
    classId: '',
    filters: [],
    searchTerm: null
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
                    const groupedStockItems = new Map<StockItemCategoryGroup, StockItemDto[]>();
                    const groupsByCategoryId = new Map<string, StockItemCategoryGroup>();

                    for (const item of result) {
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

                    patchState(store, { stockItems: result, groupedStockItems });
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
    }),
    withEventHandlers((store, events = inject(Events)) => ({
      goodsReceiptImported: events.on(realtimeEvents.goodsReceiptImportConfirmed)
        .pipe(
          map(() => store.filter()),
          tap((filter) => store.load(filter))
        )
    }))
  );
}
