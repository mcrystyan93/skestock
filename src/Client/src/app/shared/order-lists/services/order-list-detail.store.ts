import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';
import {
  CreateOrderListRequest,
  OrderListDto,
  UpdateOrderListRequest
} from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { isNil } from 'lodash-es';
import { EMPTY, of, pipe, switchMap, tap } from 'rxjs';
import { OrderListsHttp } from './order-lists.http';

type OrderListDetailState = {
  orderList: Partial<OrderListDto>;
};

const initialState: OrderListDetailState = {
  orderList: {}
};

export const NEW_ORDER_LIST_ROUTE_ID = 'new';

export const orderListApiEvents = eventGroup({
  source: 'Order List API',
  events: {
    saveSuccess: type<{ operationId: string }>(),
    saveFailure: type<{ operationId: string }>(),
    submitSuccess: type<{ operationId: string }>(),
    submitFailure: type<{ operationId: string }>()
  }
});

export const OrderListDetailState = signalStore(
  withState(initialState),
  withLoadingFeature('orderList'),
  withProblemDetailsFeature('orderList'),
  withProps(() => ({
    orderListHttp: inject(OrderListsHttp),
    dispatcher: injectDispatch(orderListApiEvents)
  })),
  withMethods((store) => {
    const hasValidId = (id: string | null | undefined): id is string =>
      typeof id === 'string' && id.trim().length > 0;

    const getCurrentOrderListId = (operationId: string) => {
      const id = store.orderList().id;

      if (!hasValidId(id)) {
        store.handleOrderListError({ title: 'Order list ID missing', status: 400 });
        store.setOrderListLoaded();
        store.dispatcher.saveFailure({ operationId });
        return null;
      }

      return id;
    };

    const loadOrderList = rxMethod<LoadOrderListRequest>(
      pipe(
        tap(() => {
          store.setOrderListLoading();
          store.clearOrderListErrors();
        }),
        switchMap(({ id, prefill }) => {
          if (id === NEW_ORDER_LIST_ROUTE_ID) {
            patchState(store, {
              orderList: prefill ?? {}
            });
            store.setOrderListLoaded();
            return of(null);
          }

          if (!hasValidId(id)) {
            store.handleOrderListError({ title: 'Order list ID missing', status: 400 });
            store.setOrderListLoaded();
            return of(null);
          }

          return store.orderListHttp.getById(id).pipe(
            mapResponse({
              next: (orderList) => {
                patchState(store, { orderList });
                store.setOrderListLoaded();
              },
              error: (error) => {
                store.handleOrderListError(error);
                store.setOrderListLoaded();
              }
            })
          );
        })
      )
    );

    const createOrderList = rxMethod<SaveOrderListOperation<CreateOrderListRequest>>(
      pipe(
        tap(() => {
          store.setOrderListLoading();
          store.clearOrderListErrors();
        }),
        switchMap(({ request, operationId }) =>
          store.orderListHttp.create(request).pipe(
            mapResponse({
              next: (orderList) => {
                patchState(store, { orderList });
                store.dispatcher.saveSuccess({ operationId });
                store.setOrderListLoaded();
              },
              error: (error) => {
                store.handleOrderListError(error);
                store.dispatcher.saveFailure({ operationId });
                store.setOrderListLoaded();
              }
            })
          )
        )
      )
    );

    const updateOrderList = rxMethod<SaveOrderListOperation<UpdateOrderListRequest>>(
      pipe(
        tap(() => {
          store.setOrderListLoading();
          store.clearOrderListErrors();
        }),
        switchMap(({ request, operationId }) => {
          const id = getCurrentOrderListId(operationId);

          if (isNil(id)) {
            return EMPTY;
          }

          return store.orderListHttp.update(id, request).pipe(
            mapResponse({
              next: (orderList) => {
                patchState(store, { orderList });
                store.setOrderListLoaded();
                store.dispatcher.saveSuccess({ operationId });
              },
              error: (error) => {
                store.handleOrderListError(error);
                store.dispatcher.saveFailure({ operationId });
                store.setOrderListLoaded();
              }
            })
          );
        })
      )
    );

    const saveOrderList = (
      request: CreateOrderListRequest | UpdateOrderListRequest,
      operationId: string
    ): boolean => {
      if (!isNil(store.orderList().id)) {
        updateOrderList({
          request: {
            name: request.name,
            note: request.note,
            lines: request.lines
          },
          operationId
        });
        return true;
      }

      if (!('classId' in request) || !hasValidId(request.classId)) {
        store.handleOrderListError({ title: 'Class ID missing', status: 400 });
        return false;
      }

      createOrderList({ request, operationId });
      return true;
    };

    const submitOrderList = rxMethod<{ operationId: string }>(
      pipe(
        tap(() => {
          store.setOrderListLoading();
          store.clearOrderListErrors();
        }),
        switchMap(({ operationId }) => {
          const id = store.orderList().id;

          if (!hasValidId(id)) {
            store.handleOrderListError({ title: 'Order list ID missing', status: 400 });
            store.setOrderListLoaded();
            store.dispatcher.submitFailure({ operationId });
            return EMPTY;
          }

          return store.orderListHttp.submit(id).pipe(
            mapResponse({
              next: (orderList) => {
                patchState(store, { orderList });
                store.dispatcher.submitSuccess({ operationId });
                store.setOrderListLoaded();
              },
              error: (error) => {
                store.handleOrderListError(error);
                store.dispatcher.submitFailure({ operationId });
                store.setOrderListLoaded();
              }
            })
          );
        })
      )
    );

    return { loadOrderList, saveOrderList, submitOrderList };
  })
);

type SaveOrderListOperation<TRequest> = {
  request: TRequest;
  operationId: string;
};

export type LoadOrderListRequest = {
  id: string;
  prefill?: Partial<OrderListDto> | null;
};
