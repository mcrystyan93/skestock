import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import {
  buildOrderListFilter,
  GetAllOrderListsRequest,
  OrderListListItemDto,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData
} from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { EMPTY, filter, map, pipe, switchMap, tap } from 'rxjs';
import { OrderListsHttp } from './order-lists.http';

type OrderListCollectionState = {
  orderLists: OrderListListItemDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllOrderListsRequest;
  isLoadingMore: boolean;
};

const initialState: OrderListCollectionState = {
  orderLists: [],
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

export function withOrderListCollection() {
  return signalStoreFeature(
    withState(initialState),
    withLoadingFeature('orderLists'),
    withProblemDetailsFeature('orderLists'),
    withComputed((store) => ({
      hasNextPage: () => store.paginationData()?.hasNextPage ?? false,
      nextCursor: () => store.paginationData()?.nextCursor ?? null
    })),
    withProps(() => ({
      orderListsHttp: inject(OrderListsHttp)
    })),
    withMethods((store) => {
      const load = rxMethod<GetAllOrderListsRequest>(
        pipe(
          map((data) => buildOrderListFilter(store.filter(), data)),
          tap((filter) => {
            store.clearOrderListsErrors();
            store.setOrderListsLoading();

            patchState(store, { filter, isLoadingMore: false });
          }),
          switchMap((filter) =>
            store.orderListsHttp.getAll(filter)
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      orderLists: result.data,
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort }
                    });
                    store.setOrderListsLoaded();
                  },
                  error: (error) => {
                    store.handleOrderListsError(error);
                    store.setOrderListsLoaded();
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
            store.clearOrderListsErrors();
            patchState(store, { isLoadingMore: true });
          }),
          switchMap(() => {
            const filter = store.filter();
            const nextCursor = store.nextCursor();

            if (!nextCursor) {
              patchState(store, { isLoadingMore: false });
              return EMPTY;
            }

            return store.orderListsHttp.getAll({ ...filter, cursor: nextCursor })
              .pipe(
                mapResponse({
                  next: (result) => {
                    patchState(store, {
                      orderLists: [...store.orderLists(), ...result.data],
                      paginationData: result,
                      filter: { ...filter, cursor: null, sort: result.sort },
                      isLoadingMore: false
                    });
                    store.setOrderListsLoaded();
                  },
                  error: (error) => {
                    store.handleOrderListsError(error);
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
