import { CreateItemRequest, EditItemRequest, ItemDto } from '@ske/models';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { ItemsHttp } from '@ske/shared/items';
import { isNil, isNumber, isString, toNumber } from 'lodash-es';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, of, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';

type ItemDetailState = { item: Partial<ItemDto> };
const initialState: ItemDetailState = { item: {} };
export const NEW_ITEM_ROUTE_ID = 'new';
export const itemApiEvents = eventGroup({
  source: 'Item API',
  events: {
    saveSuccess: type<void>()
  }
});
export const ItemDetailState = signalStore(
  withState(initialState),
  withLoadingFeature('item'),
  withProblemDetailsFeature('item'),
  withProps(() => ({
    itemHttp: inject(ItemsHttp),
    dispatcher: injectDispatch(itemApiEvents)
  })),
  withMethods((store) => {
    const getCurrentItemId = () => {
      const id = store.item().id;

      if (isNil(id)) {
        store.handleItemError({ title: 'Item ID missing', status: 400 });
        store.setItemLoaded();
        return null;
      }

      return id;
    };

    const loadItem = rxMethod<number | string>(
      pipe(
        tap(() => {
          store.setItemLoading();
          store.clearItemErrors();
        }),
        switchMap((id) => {
          if (isString(id) && id === NEW_ITEM_ROUTE_ID) {
            patchState(store, {
              item: {}
            });
            store.setItemLoaded();
            return of(null);
          }

          const idAsNumber = toNumber(id);

          if (!isNumber(idAsNumber) || Number.isNaN(idAsNumber)) {
            store.handleItemError({ title: 'Item ID missing', status: 400 });
            store.setItemLoaded();
            return of(null);
          }

          return store.itemHttp.getById(idAsNumber).pipe(
            mapResponse({
              next: (item) => {
                patchState(store, { item });
                store.setItemLoaded();
              },
              error: (error) => {
                store.handleItemError(error);
                store.setItemLoaded();
              }
            })
          );
        })
      )
    );

    const createItem = rxMethod<CreateItemRequest>(
      pipe(
        tap(() => {
          store.setItemLoading();
          store.clearItemErrors();
        }),
        switchMap((request) =>
          store.itemHttp.create(request).pipe(
            mapResponse({
              next: (item) => {
                patchState(store, { item });
                store.dispatcher.saveSuccess();
                store.setItemLoaded();
              },
              error: (error) => {
                store.handleItemError(error);
                store.setItemLoaded();
              }
            })
          )
        )
      )
    );

    const editItem = rxMethod<EditItemRequest>(
      pipe(
        tap(() => {
          store.setItemLoading();
          store.clearItemErrors();
        }),
        switchMap((request) => {
          const id = getCurrentItemId();

          if (isNil(id)) {
            return EMPTY;
          }

          return store.itemHttp.edit(id, request).pipe(
            mapResponse({
              next: (item) => {
                patchState(store, { item });
                store.dispatcher.saveSuccess();
                store.setItemLoaded();
              },
              error: (error) => {
                store.handleItemError(error);
                store.setItemLoaded();
              }
            })
          );
        })
      )
    );

    const saveItem = (request: CreateItemRequest | EditItemRequest) => {
      if (store.item().id) {
        editItem(request);
        return;
      }

      createItem(request);
    };

    return { loadItem, saveItem };
  })
);
