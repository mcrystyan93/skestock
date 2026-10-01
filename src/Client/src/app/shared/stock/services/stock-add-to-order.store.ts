import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import {
  AddItemToOrderListRequest,
  ColumnFilter,
  OrderListDto,
  OrderListListItemDto,
  PAGINATION_PAGE_SIZE
} from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { OrderListsHttp } from '@ske/shared/order-lists';

type StockAddToOrderState = {
  drafts: OrderListListItemDto[];
};

const initialState: StockAddToOrderState = {
  drafts: []
};

export const stockAddToOrderApiEvents = eventGroup({
  source: 'Stock Add To Order API',
  events: {
    addSuccess: type<OrderListDto>()
  }
});

/** Modal-scoped store: lists the class's draft order lists and adds a stock item to one of them. */
export const StockAddToOrderState = signalStore(
  withState(initialState),
  withLoadingFeature('drafts'),
  withProblemDetailsFeature('drafts'),
  withLoadingFeature('addToOrder'),
  withProblemDetailsFeature('addToOrder'),
  withProps(() => ({
    orderListsHttp: inject(OrderListsHttp),
    dispatcher: injectDispatch(stockAddToOrderApiEvents)
  })),
  withMethods((store) => {
    const loadDrafts = rxMethod<string>(
      pipe(
        tap(() => {
          store.setDraftsLoading();
          store.clearDraftsErrors();
        }),
        switchMap((classId) => {
          const filters: ColumnFilter[] = [
            { field: 'classId', operator: 'equals', fieldType: 'number', value: classId },
            { field: 'status', operator: 'equals', fieldType: 'string', value: 'Draft' }
          ];

          return store.orderListsHttp
            .getAll({ filters, pageSize: PAGINATION_PAGE_SIZE, cursor: null, sort: [], searchTerm: null })
            .pipe(
              mapResponse({
                next: (page) => {
                  patchState(store, { drafts: page.data });
                  store.setDraftsLoaded();
                },
                error: (error) => {
                  store.handleDraftsError(error);
                  store.setDraftsLoaded();
                }
              })
            );
        })
      )
    );

    const addItem = rxMethod<AddItemToOrderListRequest>(
      pipe(
        tap(() => {
          store.setAddToOrderLoading();
          store.clearAddToOrderErrors();
        }),
        switchMap((request) =>
          store.orderListsHttp.addItem(request).pipe(
            mapResponse({
              next: (orderList) => {
                store.setAddToOrderLoaded();
                store.dispatcher.addSuccess(orderList);
              },
              error: (error) => {
                store.handleAddToOrderError(error);
                store.setAddToOrderLoaded();
              }
            })
          )
        )
      )
    );

    return { loadDrafts, addItem };
  })
);
