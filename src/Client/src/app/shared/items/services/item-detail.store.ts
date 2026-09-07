import { CreateItemRequest, EditItemRequest, ItemDto } from '@ske/models';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
// noinspection ES6PreferShortImport
import { ItemsHttp } from '../services/items.http';
import { isNil } from 'lodash-es';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, of, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';

type ItemDetailState = { item: Partial<ItemDto> };
const initialState: ItemDetailState = { item: {} };
export const NEW_ITEM_ROUTE_ID = 'new';
export type LoadItemRequest = { id: string; prefill?: Partial<ItemDto> | null };
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

    const loadItem = rxMethod<LoadItemRequest>(
      pipe(
        tap(() => {
          store.setItemLoading();
          store.clearItemErrors();
        }),
        switchMap(({ id, prefill }) => {
          if (id === NEW_ITEM_ROUTE_ID) {
            patchState(store, {
              item: prefill ?? {}
            });
            store.setItemLoaded();
            return of(null);
          }

          if (isNil(id) || id === '') {
            store.handleItemError({ title: 'Item ID missing', status: 400 });
            store.setItemLoaded();
            return of(null);
          }

          return store.itemHttp.getById(id).pipe(
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
