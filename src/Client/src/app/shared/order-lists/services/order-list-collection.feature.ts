import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStoreFeature, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import {
  buildOrderListFilter,
  GetAllOrderListsRequest,
  OrderListListItemDto,
  OrderListStatusAction,
  OrderListStatusChange,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData
} from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { NzMessageService } from 'ng-zorro-antd/message';
import { EMPTY, filter, map, pipe, switchMap, tap } from 'rxjs';
import { OrderListsHttp } from './order-lists.http';

type OrderListCollectionState = {
  orderLists: OrderListListItemDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllOrderListsRequest;
  isLoadingMore: boolean;
  statusChangingId: string | null;
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
  isLoadingMore: false,
  statusChangingId: null
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
      orderListsHttp: inject(OrderListsHttp),
      nzMessageService: inject(NzMessageService)
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

      const changeStatus = rxMethod<OrderListStatusChange>(
        pipe(
          filter(() => store.statusChangingId() === null),
          tap(({ id }) => {
            store.clearOrderListsErrors();
            patchState(store, { statusChangingId: id });
          }),
          switchMap(({ id, action }) => {
            const request = action === 'submit'
              ? store.orderListsHttp.submit(id)
              : action === 'cancel'
                ? store.orderListsHttp.cancel(id)
                : store.orderListsHttp.reopen(id);

            return request.pipe(
              mapResponse({
                next: () => {
                  patchState(store, { statusChangingId: null });
                  store.nzMessageService.success(STATUS_CHANGE_MESSAGES[action]);
                  load(store.filter());
                },
                error: (error) => {
                  store.handleOrderListsError(error);
                  patchState(store, { statusChangingId: null });
                }
              })
            );
          })
        )
      );

      return { load, loadMore, changeStatus };
    })
  );
}

const STATUS_CHANGE_MESSAGES: Record<OrderListStatusAction, string> = {
  submit: 'Comanda a fost aprobată.',
  cancel: 'Comanda a fost anulată.',
  reopen: 'Comanda a fost redeschisă ca ciornă.'
};
