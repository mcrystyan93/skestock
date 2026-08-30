import {
  ItemDto,
  GetAllItemsRequest,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData, buildItemListFilter
} from '@ske/models';
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { ItemsHttp } from '@ske/shared/items';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, filter, map, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';


type ItemCollectionState = {
  items: ItemDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllItemsRequest;
  isLoadingMore: boolean;
};

const initialState: ItemCollectionState = {
  items: [],
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

export function withItemCollection() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('items'),
    withProblemDetailsFeature('items'),
    withComputed((store) => ({
      hasNextPage: () => store.paginationData()?.hasNextPage ?? false,
      nextCursor: () => store.paginationData()?.nextCursor ?? null
    })),
    withProps(() => ({
      itemHttp: inject(ItemsHttp)
    })),
    withMethods((store) => {
      const load = rxMethod<GetAllItemsRequest>(
        pipe(
          map(data => buildItemListFilter(store.filter(), data)),
          tap((filter) => {
            store.clearItemsErrors();
            store.itemsLoading();

            patchState(store, { filter, isLoadingMore: false });
          }),
          switchMap(filter =>
            store.itemHttp.getAll(filter)
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      items: result.data,
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort }
                    });
                  },
                  error: (error) => {
                    store.handleItemsError(error);
                    store.setItemsLoaded();
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
            store.clearItemsErrors();
            patchState(store, { isLoadingMore: true });
          }),
          switchMap(() => {
            const filter = store.filter();
            const nextCursor = store.nextCursor();

            if (!nextCursor) {
              patchState(store, { isLoadingMore: false });
              return EMPTY;
            }

            return store.itemHttp.getAll({ ...filter, cursor: nextCursor })
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      items: [...store.items(), ...result.data],
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort },
                      isLoadingMore: false
                    });
                  },
                  error: (error) => {
                    store.handleItemsError(error);
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
